using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using ScottPlot;
public class EcgDataInput
{
    [LoadColumn(0, 159), VectorType(160)]
    public float[] Features { get; set; }

    [LoadColumn(160)]
    public string Diagnosis { get; set; }
}


public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("--- Початок процесу навчання моделі ЕКГ ---");

        // Шляхи беремо з appsettings.json поруч з .exe (bin/...), а не з поточної папки,
        // тому програма працює незалежно від того, звідки її запустили.
        // appsettings.Development.json (в .gitignore) — для локальних шляхів, перекриває appsettings.json.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var combinedDatasetPath = config["Paths:DatasetCsv"]
            ?? throw new InvalidOperationException("Paths:DatasetCsv не задано в appsettings.json");
        var outputDirectory = config["Paths:OutputDirectory"]
            ?? throw new InvalidOperationException("Paths:OutputDirectory не задано в appsettings.json");
        var modelOutputPath = Path.Combine(outputDirectory, "EcgClassifier.zip");

        Directory.CreateDirectory(outputDirectory);
        var mlContext = new MLContext(seed: 1);

        // 1. Завантаження
        Console.WriteLine($"[Крок 1/5] Завантаження датасету з: {combinedDatasetPath}");
        var allData = mlContext.Data.LoadFromTextFile<EcgDataInput>(
            combinedDatasetPath, hasHeader: true, separatorChar: ',');

        // 2. Конвеєр
        Console.WriteLine("[Крок 2/5] Побудова конвеєра (MapValueToKey → LightGbm → MapKeyToValue)...");
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: "Diagnosis")
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // 3. Основна оцінка — 5-fold cross-validation.
        // Даних мало (~491 зображення, MI лише 30), тому один split 80/20 дає "лотерейну" оцінку.
        // CV навчає модель 5 разів; кожне зображення рівно один раз потрапляє в test.
        Console.WriteLine("[Крок 3/5] 5-fold cross-validation...");
        EvaluateWithCrossValidation(mlContext, pipeline, allData, numberOfFolds: 5);

        // 4. Старий спосіб (один split 80/20) — для порівняння з результатом у дипломі + криві навчання.
        Console.WriteLine("\n[Крок 4/5] Оцінка на одному split 80/20 (для порівняння)...");
        var split = mlContext.Data.TrainTestSplit(allData, testFraction: 0.2, seed: 1);
        var splitModel = pipeline.Fit(split.TrainSet);
        var splitMetrics = mlContext.MulticlassClassification.Evaluate(
            splitModel.Transform(split.TestSet), "Label", "Score");

        Console.WriteLine($"Точність (Micro): {splitMetrics.MicroAccuracy:P2}");
        Console.WriteLine($"Точність (Macro): {splitMetrics.MacroAccuracy:P2}");
        Console.WriteLine(splitMetrics.ConfusionMatrix.GetFormattedConfusionTable());

        Console.WriteLine("\n--- Побудова кривих навчання ---");
        BuildLearningCurves(mlContext, pipeline, split.TrainSet, split.TestSet, outputDirectory);

        // 5. Фінальна модель. CV і split лише ОЦІНЮЮТЬ якість;
        // модель для API вчимо на всіх даних — більше прикладів, особливо для малого класу MI.
        Console.WriteLine("\n[Крок 5/5] Навчання фінальної моделі на всіх даних...");
        var finalModel = pipeline.Fit(allData);
        mlContext.Model.Save(finalModel, allData.Schema, modelOutputPath);
        Console.WriteLine($"-> Модель збережено: {modelOutputPath}");
    }

    private class HoldOutPrediction
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string PredictedLabel { get; set; } = string.Empty;
    }

    private static void EvaluateWithCrossValidation(
        MLContext mlContext,
        IEstimator<ITransformer> pipeline,
        IDataView data,
        int numberOfFolds)
    {
        var cvResults = mlContext.MulticlassClassification.CrossValidate(
            data, pipeline, numberOfFolds: numberOfFolds, labelColumnName: "Label", seed: 1);

        foreach (var fold in cvResults)
        {
            Console.WriteLine($"  Fold {fold.Fold + 1}: Micro {fold.Metrics.MicroAccuracy:P2} | Macro {fold.Metrics.MacroAccuracy:P2}");
        }

        var micro = cvResults.Select(r => r.Metrics.MicroAccuracy).ToArray();
        var macro = cvResults.Select(r => r.Metrics.MacroAccuracy).ToArray();
        Console.WriteLine($"\nCV Micro: {micro.Average():P2} ± {StandardDeviation(micro):P2}");
        Console.WriteLine($"CV Macro: {macro.Average():P2} ± {StandardDeviation(macro):P2}");

        // Точність по класах (recall): кожне зображення було в test рівно один раз,
        // тому об'єднуємо прогнози всіх folds і рахуємо по всьому датасету.
        // Беремо рядкові назви класів, а не ключі: порядок ключів може відрізнятись між folds.
        var allHoldOut = cvResults
            .SelectMany(r => mlContext.Data.CreateEnumerable<HoldOutPrediction>(r.ScoredHoldOutSet, reuseRowObject: false))
            .ToList();

        Console.WriteLine("\nТочність по класах (усі folds разом):");
        foreach (var group in allHoldOut.GroupBy(p => p.Diagnosis).OrderBy(g => g.Key))
        {
            int correct = group.Count(p => p.PredictedLabel == p.Diagnosis);
            Console.WriteLine($"  {group.Key,-22} {correct,3}/{group.Count(),-3} = {(double)correct / group.Count():P2}");
        }
    }

    private static double StandardDeviation(double[] values)
    {
        double mean = values.Average();
        return Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / values.Length);
    }

    private static void BuildLearningCurves(
    MLContext mlContext,
    IEstimator<ITransformer> pipeline,
    IDataView fullTrainData,
    IDataView testData,
    string outputDirectory)
{
    var sizes = new[] { 0.1, 0.3, 0.5, 0.7, 1.0 };
    var trainResults = new List<double>();
    var testResults = new List<double>();
    var actualSizes = new List<int>();

    var allRows = mlContext.Data.CreateEnumerable<EcgDataInput>(fullTrainData, reuseRowObject: false).ToList();
    int total = allRows.Count;

    foreach (var frac in sizes)
    {
        int count = (int)(total * frac);
        var sampleRows = allRows.Take(count).ToList();

        // Перевірка кількості класів
        int classCount = sampleRows.Select(r => r.Diagnosis).Distinct().Count();
        if (classCount < 2)
        {
            Console.WriteLine($"[{frac * 100,4:F0}%] Пропущено — недостатньо класів ({classCount}).");
            continue;
        }

        var sample = mlContext.Data.LoadFromEnumerable(sampleRows);

        try
        {
            var model = pipeline.Fit(sample);

            var trainPred = model.Transform(sample);
            var trainMetrics = mlContext.MulticlassClassification.Evaluate(trainPred, "Label", "Score");

            var testPred = model.Transform(testData);
            var testMetrics = mlContext.MulticlassClassification.Evaluate(testPred, "Label", "Score");

            trainResults.Add(trainMetrics.MicroAccuracy);
            testResults.Add(testMetrics.MicroAccuracy);
            actualSizes.Add(count);

            Console.WriteLine(
                $"[{frac * 100,4:F0}%] Train: {trainMetrics.MicroAccuracy:P2} | Test: {testMetrics.MicroAccuracy:P2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{frac * 100,4:F0}%] Помилка під час навчання: {ex.Message}");
        }
    }

    // Експорт у CSV
    var csvPath = Path.Combine(outputDirectory, "learning_curves.csv");
    using (var writer = new StreamWriter(csvPath))
    {
        writer.WriteLine("Fraction,SampleSize,TrainAccuracy,TestAccuracy");
        for (int i = 0; i < trainResults.Count; i++)
        {
            writer.WriteLine($"{sizes[i]},{actualSizes[i]},{trainResults[i]},{testResults[i]}");
        }
    }

    Console.WriteLine($"\nДані для кривих навчання збережено у: {csvPath}");
    
    // Візуалізація
    if (trainResults.Count > 0)
    {
        PlotLearningCurves(actualSizes, trainResults, testResults, outputDirectory);
    }
}

private static void PlotLearningCurves(List<int> sizes, List<double> trainAccuracies, List<double> testAccuracies, string outputDirectory)
{
    var plt = new Plot();
    
    // Конвертуємо дані
    double[] xValues = sizes.Select(s => (double)s).ToArray();
    double[] trainValues = trainAccuracies.Select(a => a * 100).ToArray();
    double[] testValues = testAccuracies.Select(a => a * 100).ToArray();
    
    // Додаємо область між кривими (показує перенавчання)
    var fillPlot = plt.Add.FillY(xValues, trainValues, testValues);
    fillPlot.FillColor = ScottPlot.Color.FromHex("#95a5a6").WithAlpha(0.2);
    fillPlot.LineWidth = 0;
    fillPlot.LegendText = "Розрив (overfitting)";
    
    // Додаємо лінію тренувальної точності
    var trainScatter = plt.Add.Scatter(xValues, trainValues);
    trainScatter.LegendText = "Тренувальна точність";
    trainScatter.LineWidth = 3;
    trainScatter.MarkerSize = 10;
    trainScatter.MarkerShape = MarkerShape.FilledCircle;
    trainScatter.Color = ScottPlot.Color.FromHex("#3498db"); // Синій
    
    // Додаємо лінію тестової точності
    var testScatter = plt.Add.Scatter(xValues, testValues);
    testScatter.LegendText = "Тестова точність";
    testScatter.LineWidth = 3;
    testScatter.MarkerSize = 10;
    testScatter.MarkerShape = MarkerShape.FilledDiamond;
    testScatter.Color = ScottPlot.Color.FromHex("#e74c3c"); // Червоний
    
    // Додаємо горизонтальну лінію для бажаної точності (наприклад, 80%)
    var targetLine = plt.Add.HorizontalLine(80);
    targetLine.LineWidth = 2;
    targetLine.LinePattern = LinePattern.Dashed;
    targetLine.Color = ScottPlot.Color.FromHex("#27ae60"); // Зелений
    targetLine.LegendText = "Цільова точність (80%)";
    
    // Налаштування осей
    plt.Title("Криві навчання моделі класифікації ЕКГ");
    plt.XLabel("Розмір тренувального набору (зразків)");
    plt.YLabel("Точність (%)");
    
    // Налаштування легенди
    plt.ShowLegend(Alignment.LowerRight);
    
    // Обмеження осей
    plt.Axes.SetLimitsY(0, 105);
    
    // Налаштування сітки
    plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#bdc3c7");
    plt.Grid.MajorLineWidth = 1;
    plt.Grid.MinorLineColor = ScottPlot.Color.FromHex("#ecf0f1");
    
    // Стилізація графіку
    plt.FigureBackground.Color = ScottPlot.Color.FromHex("#ffffff");
    plt.DataBackground.Color = ScottPlot.Color.FromHex("#f8f9fa");
    
    // Збереження
    var outputPath = Path.Combine(outputDirectory, "learning_curves.png");
    plt.SavePng(outputPath, 1200, 800);
    Console.WriteLine($"Графік збережено у: {outputPath}");
    
    // Додаткова діагностика
    Console.WriteLine("\n==================================================");
    Console.WriteLine("      АНАЛІЗ КРИВИХ НАВЧАННЯ      ");
    Console.WriteLine("==================================================");
    
    if (trainAccuracies.Count > 0)
    {
        var lastTrainAcc = trainAccuracies.Last();
        var lastTestAcc = testAccuracies.Last();
        var gap = lastTrainAcc - lastTestAcc;
        
        Console.WriteLine($"Фінальна тренувальна точність: {lastTrainAcc:P2}");
        Console.WriteLine($"Фінальна тестова точність:     {lastTestAcc:P2}");
        Console.WriteLine($"Розрив (overfitting gap):      {gap:P2}");
        Console.WriteLine("--------------------------------------------------");
        
        if (gap > 0.2)
        {
            Console.WriteLine("❌ СИЛЬНЕ ПЕРЕНАВЧАННЯ!");
            Console.WriteLine("   Рекомендації:");
            Console.WriteLine("   • Збільшити регуляризацію");
            Console.WriteLine("   • Зменшити складність моделі");
            Console.WriteLine("   • Зібрати більше даних");
            Console.WriteLine("   • Використати data augmentation");
        }
        else if (gap > 0.1)
        {
            Console.WriteLine("⚠️  Помірне перенавчання");
            Console.WriteLine("   Рекомендації:");
            Console.WriteLine("   • Налаштувати гіперпараметри");
            Console.WriteLine("   • Використати cross-validation");
        }
        else if (lastTestAcc < 0.6)
        {
            Console.WriteLine("⚠️  НЕДОНАВЧАННЯ!");
            Console.WriteLine("   Рекомендації:");
            Console.WriteLine("   • Збільшити складність моделі");
            Console.WriteLine("   • Покращити якість ознак");
            Console.WriteLine("   • Збільшити кількість епох");
            Console.WriteLine("   • Перевірити якість даних");
        }
        else
        {
            Console.WriteLine("✅ Модель добре збалансована!");
        }
        
        // Аналіз тренду
        Console.WriteLine("\n--------------------------------------------------");
        Console.WriteLine("Тренд покращення:");
        
        if (testAccuracies.Count >= 2)
        {
            var improvement = testAccuracies.Last() - testAccuracies.First();
            Console.WriteLine($"  Покращення точності: {improvement:P2}");
            
            if (testAccuracies.Count >= 3)
            {
                var recent = testAccuracies.Skip(testAccuracies.Count - 3).ToList();
                var variance = CalculateVariance(recent);
                
                if (variance < 0.001)
                {
                    Console.WriteLine("  📊 Модель досягла плато (стабілізувалась)");
                }
                else
                {
                    Console.WriteLine("  📈 Модель все ще покращується");
                }
            }
        }
    }
    
    Console.WriteLine("==================================================\n");
}

private static double CalculateVariance(List<double> values)
{
    if (values.Count < 2) return 0;
    
    double mean = values.Average();
    double sumSquaredDiff = values.Sum(v => Math.Pow(v - mean, 2));
    return sumSquaredDiff / values.Count;
}
}
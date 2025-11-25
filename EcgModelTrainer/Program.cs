using System.Text;
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
    private static readonly string CombinedDatasetPath = @"D:\ECG_Dataset_Features\dataset_combined.csv";
    private static readonly string ModelOutputPath = @"D:\ECG_Models\EcgClassifier.zip";

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("--- Початок процесу навчання моделі ЕКГ ---");

        Directory.CreateDirectory(Path.GetDirectoryName(ModelOutputPath)!);
        var mlContext = new MLContext(seed: 1);

        // 1. Завантаження
        Console.WriteLine($"[Крок 1/5] Завантаження датасету з: {CombinedDatasetPath}");
        var allData = mlContext.Data.LoadFromTextFile<EcgDataInput>(
            CombinedDatasetPath, hasHeader: true, separatorChar: ',');

        // 2. Розділення
        Console.WriteLine("[Крок 2/5] Розділення даних...");
        var split = mlContext.Data.TrainTestSplit(allData, testFraction: 0.2, seed: 1);
        var trainDataFull = split.TrainSet;
        var testData = split.TestSet;

        // 3. Конвеєр
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: "Diagnosis")
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));


        // 4. Основне навчання
        Console.WriteLine("[Крок 4/5] Основне навчання моделі...");
        var model = pipeline.Fit(trainDataFull);
        Console.WriteLine("-> Навчання завершено.");

        // 5. Оцінка
        Console.WriteLine("[Крок 5/5] Оцінка якості...");
        var predictions = model.Transform(testData);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score");

        Console.WriteLine($"\nТочність (Micro): {metrics.MicroAccuracy:P2}");
        Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

        // 6. Криві навчання
        Console.WriteLine("\n--- Побудова кривих навчання ---");
        BuildLearningCurves(mlContext, pipeline, trainDataFull, testData);

        // 7. Збереження
        mlContext.Model.Save(model, trainDataFull.Schema, ModelOutputPath);
        Console.WriteLine("\n-> Модель збережено успішно.");
    }

    private static void BuildLearningCurves(
    MLContext mlContext,
    IEstimator<ITransformer> pipeline,
    IDataView fullTrainData,
    IDataView testData)
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
    var csvPath = @"D:\ECG_Models\learning_curves.csv";
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
        PlotLearningCurves(actualSizes, trainResults, testResults);
    }
}

private static void PlotLearningCurves(List<int> sizes, List<double> trainAccuracies, List<double> testAccuracies)
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
    var outputPath = @"D:\ECG_Models\learning_curves.png";
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
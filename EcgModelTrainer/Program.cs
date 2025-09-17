using Microsoft.ML;
using Microsoft.ML.Data;

public class EcgDataInput
{
    [LoadColumn(0, 159), VectorType(160)]
    public float[] Features { get; set; }

    [LoadColumn(160)]
    public string Diagnosis { get; set; }
}

public class EcgPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedDiagnosis { get; set; }
    public float[] Score { get; set; }
}

public class Program
{
    private static readonly string CombinedDatasetPath = @"D:\ECG_Dataset_Features\dataset_combined.csv";
    private static readonly string ModelOutputPath = @"D:\ECG_Models\EcgClassifier.zip";

    public static void Main(string[] args)
    {
        Console.WriteLine("--- Початок процесу навчання моделі ЕКГ ---");
        
        Directory.CreateDirectory(Path.GetDirectoryName(ModelOutputPath)!);
        
        var mlContext = new MLContext(seed: 1);

        // 1. ЗАВАНТАЖЕННЯ ДАНИХ
        Console.WriteLine($"[Крок 1/5] Завантаження датасету з: {CombinedDatasetPath}");
        IDataView allData = mlContext.Data.LoadFromTextFile<EcgDataInput>(CombinedDatasetPath, hasHeader: true, separatorChar: ',');

        // 2. РОЗДІЛЕННЯ ДАНИХ
        Console.WriteLine("[Крок 2/5] Розділення даних...");
        var trainTestSplit = mlContext.Data.TrainTestSplit(allData, testFraction: 0.2, seed: 1);
        IDataView trainData = trainTestSplit.TrainSet;
        IDataView testData = trainTestSplit.TestSet;
        
        // 3. ПОБУДОВА КОНВЕЄРА НАВЧАННЯ
        Console.WriteLine("[Крок 3/5] Побудова конвеєра навчання...");
        var pipeline =
            // Крок 3.1: Перетворюємо текстовий "Diagnosis" на числовий "Label".
            // ВХІД: "Diagnosis", ВИХІД: "Label"
            mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Diagnosis", outputColumnName: "Label")
            
            // Крок 3.2: Алгоритм навчання.
            // ВХІД: "Label", "Features". ВИХІД: "PredictedLabel" (числовий), "Score"
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm(labelColumnName: "Label", featureColumnName: "Features"))
            
            // Крок 3.3: Перетворюємо числовий прогноз "PredictedLabel" назад у текст.
            // ВХІД: "PredictedLabel", ВИХІД: "PredictedLabel" (тепер текстовий)
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // 4. НАВЧАННЯ МОДЕЛІ
        Console.WriteLine("[Крок 4/5] Початок навчання моделі...");
        var trainedModel = pipeline.Fit(trainData);
        Console.WriteLine("-> Навчання завершено.");
        
        // 5. ОЦІНКА ЯКОСТІ МОДЕЛІ
        Console.WriteLine("[Крок 5/5] Оцінка якості моделі...");
        var predictions = trainedModel.Transform(testData);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

        Console.WriteLine("\n==================================================");
        Console.WriteLine("      МЕТРИКИ ЯКОСТІ МОДЕЛІ      ");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($" Загальна точність (MicroAccuracy): {metrics.MicroAccuracy:P2}");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
        Console.WriteLine("==================================================");
        
        // 6. ЗБЕРЕЖЕННЯ МОДЕЛІ
        Console.WriteLine($"\nЗбереження моделі в: {ModelOutputPath}");
        mlContext.Model.Save(trainedModel, trainData.Schema, ModelOutputPath);
        Console.WriteLine("-> Модель успішно збережено.");
    }
}
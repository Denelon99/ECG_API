using Microsoft.ML.Data;

namespace ECG_API.Core.MLModels
{
        public class ModelInput
        {
            [VectorType(160)] // 160 ознак
            public float[] Features { get; set; }
            public string Diagnosis { get; set; } = string.Empty;
        }

        public class ModelOutput
        {
            [ColumnName("PredictedLabel")]
            public string PredictedDiagnosis { get; set; }
            
            public float[] Score { get; set; }
        }
}

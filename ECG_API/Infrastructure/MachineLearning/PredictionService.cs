using Microsoft.Extensions.ML;
using ECG_API.Core.Services;
using ECG_API.Core.MLModels;

namespace ECG_API.Infrastructure.MachineLearning
{
    public class PredictionService : IPredictionService
    {
        private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;

        public PredictionService(PredictionEnginePool<ModelInput, ModelOutput> pool)
        {
            _pool = pool;
        }

        public ModelOutput Predict(ModelInput input)
        {
            return _pool.Predict(modelName: "EcgClassificationModel", example: input);
        }
    }
}
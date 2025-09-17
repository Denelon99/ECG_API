using ECG_API.Core.MLModels;

namespace ECG_API.Core.Services
{
    public interface IPredictionService
    {
        ModelOutput Predict(ModelInput input);
    }
}

# ECG Image Classifier

A .NET 9 Web API that takes a photo or scan of a 12-lead ECG printout, cleans it with computer vision, turns it into a 160-number feature vector and classifies it with an ML.NET LightGBM model into one of 4 classes: **Normal**, **Myocardial Infarction**, **Abnormal Heartbeat**, **History of MI**.

Built as my master's thesis at Lviv Polytechnic National University (supervisor: Roman Melnyk), then reworked: runs on Linux, honest evaluation, analysis history in SQLite.

> ⚠️ This is a learning project, **not a medical device**. Do not use it for diagnosis.

## How it works

1. **Crop** the working area of the printout.
2. **Remove the grid** and isolate the signal, paste it on a white background.
3. **Remove artifacts**: calibration pulse, gain lines, text.
4. **Reconnect** broken signal lines.
5. **Split** the image into 4 horizontal strips.
6. **Extract features**: for each strip find the baseline, split the strip into 20 fragments and measure the area above and below the baseline → 40 features per strip, **160** in total.
7. **Classify** the vector with a LightGBM model (ML.NET).
8. **Save** the result to SQLite and return a PNG visualization (baselines + areas) with the diagnosis in a response header.

![Example result](docs/example-result.png)

## Tech stack

| Area             | Tools                                       |
| ---------------- | ------------------------------------------- |
| API              | ASP.NET Core 9 Web API, Swagger             |
| Computer vision  | OpenCvSharp 4.13, SixLabors.ImageSharp      |
| Machine learning | ML.NET 4 (LightGBM), `PredictionEnginePool` |
| Database         | EF Core 9 + SQLite, migrations              |
| Charts           | ScottPlot (learning curves)                 |
| Platform         | .NET 9, Linux (tested on Fedora)            |

## Project structure

```
ECG_API.sln
├── ECG_API/            # Web API + the whole image-processing pipeline
│   ├── Controllers/    # /analyze and /history endpoints
│   ├── Core/           # service interfaces, models, ML schema
│   └── Infrastructure/ # OpenCV / ML.NET / EF Core / storage implementations
├── EcgDataGenerator/   # console app: image dataset → features CSV (reuses ECG_API services)
└── EcgModelTrainer/    # console app: CSV → cross-validation → EcgClassifier.zip
```

The generator calls the same pipeline as the API (shared DI registration `AddEcgProcessing()`), so training features and runtime features are computed identically.

## Getting started (Linux)

The trained model and the dataset are not in the repo (dataset is 589 MB), so the first run takes 3 steps: get data → generate features → train.

**1. Prerequisites:** .NET SDK 9.0.1xx (`global.json` pins `9.0.100` with patch roll-forward).

**2. Clone and download the dataset**

```bash
git clone https://github.com/Denelon99/ECG_API.git
cd ECG_API
```

Download [ECG Images dataset of Cardiac Patients](https://doi.org/10.17632/gwbz3fsgp8.2) (Mendeley Data) and put its 4 class folders into `data/dataset/`:

```
data/dataset/
├── ECG Images of Myocardial Infarction Patients (240x12=2880)/
├── ECG Images of Patient that have abnormal heartbeat (233x12=2796)/
├── ECG Images of Patient that have History of MI (172x12=2064)/
└── Normal Person ECG Images (284x12=3408)/
```

`data/` is gitignored. To keep the data somewhere else, create `appsettings.Development.json` next to `appsettings.json` in `EcgDataGenerator/` and `EcgModelTrainer/` with your own absolute paths — it overrides the defaults and is gitignored.

**3. Generate features and train the model** (run from the repo root)

```bash
dotnet run --project EcgDataGenerator -c Release   # → data/features/dataset_combined.csv (~8 min)
dotnet run --project EcgModelTrainer -c Release    # → data/models/EcgClassifier.zip
cp data/models/EcgClassifier.zip ECG_API/Core/MLModels/
```

**4. Run the API**

```bash
dotnet run --project ECG_API --launch-profile http
```

Swagger UI: http://localhost:5209/swagger. The SQLite database (`ECG_API/ecg.db`) is created and migrated automatically on startup.

## API usage

**Analyze an image** — `POST /api/analysis/analyze`, `multipart/form-data`, field `ImageFile`:

```bash
curl -s -D - -o result.png \
  -F "ImageFile=@data/dataset/Normal Person ECG Images (284x12=3408)/Normal(1).jpg" \
  http://localhost:5209/api/analysis/analyze
```

The body is the PNG visualization; the diagnosis is in the headers:

```
X-ECG-Predicted-Diagnosis: Normal
X-ECG-Feature-Count: 160
```

**History** — `GET /api/analysis/history`, newest first:

```bash
curl -s http://localhost:5209/api/analysis/history
```

```json
[{"id":1,"createdAtUtc":"2026-10-03T16:28:37.9776994","originalFileName":"PMI(64).jpg","predictedDiagnosis":"HistoryOfMI","confidence":0.9935852,"resultImageFileName":"full_result_20261003_192837897.png"}]
```

## Results & limitations

The thesis reported **96% accuracy**. While moving the project to Linux I re-checked the data and found that number was inflated:

- **Duplicates.** The dataset has 928 files but only **491 unique** images (byte-identical copies, checked by hash). Myocardial Infarction: 239 files → 30 unique.
- **Data leakage.** A random train/test split puts copies of the same image into both train and test, so the model is tested on images it has already seen.

Fix: the generator now skips duplicates (SHA-256), and the trainer uses **5-fold cross-validation** with **macro accuracy** (classes are imbalanced after dedupe).

| Run | Data | Evaluation | Micro acc. | Macro acc. |
|---|---|---|---|---|
| Thesis | manually cleaned | 80/20 split | 96% | — |
| Now | 491 unique | 5-fold CV | **74.1% ± 4.4%** | **57.8% ± 4.6%** |

Per-class recall (CV, all folds pooled):

| Class | Recall |
|---|---|
| Abnormal Heartbeat | 208/233 = 89% |
| Normal | 116/142 = 82% |
| History of MI | 36/86 = 42% |
| Myocardial Infarction | 5/30 = 17% |

Limitations:

- **Overfitting:** train accuracy is 100%.
- **Myocardial Infarction is poorly recognized** — only 30 unique images. Class weights were tried and gave no real improvement.
- One dataset, one printout format; not validated on real-world photos.

## Credits

- Dataset: Khan, A. H., Hussain, M. (2021). *ECG Images dataset of Cardiac Patients*, Mendeley Data, V2. DOI: [10.17632/gwbz3fsgp8.2](https://doi.org/10.17632/gwbz3fsgp8.2). License: CC BY 4.0.
- Author: Denys Melnyk ([LinkedIn](https://www.linkedin.com/in/denys-melnyk-9276b032a))
- Thesis supervisor: Roman Melnyk, Lviv Polytechnic National University

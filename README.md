# Use case 1: AI-POWERED “COMMAND CENTER”: MULTI-SENSORY LEARNING ANALYSIS
**FOCUS:** Build a Digital Command Center that uses Computer Vision and Natural Language Processing (NLP) to capture multi-sensory data during learning sessions. This helps professionals understand engagement levels and session dynamics in real-time, allowing them to focus on the individual rather than the clipboard.

**Hackable Outcome:** A dashboard that synthesizes video/audio into actionable “session health” metrics.

**GitHub:** https://github.com/fsi-hack4autism/uc1-multisensory-learning-analysis

| Name | Role | Company |
|------|------|---------|
| Rishi Bhatnagar | Use Case Lead | LPL Financial |
| Amy Backes | Subject Matter Expert | BCBA |
| Rob Reese | Tech Lead | Microsoft |


Output behaviors to be shown on the dashboard:
## How well is the student following the context of the conversation
1. Transcribe the video
2. Identify the speakers
3. Identify the context of the full conversation
4. How well does each utterance of student follow the conversation
   
## What is the student's emotion during the conversation - e.g., angry, frustrated, happy, etc.
1. Speed, Decibel, ans tremble of the voice
2. identify speakers
3. look for words that demonstrate emotions

## Is the student exhibiting any stimming behavior - e.g., scripting (repeating themselves), echoing the other speakers
1. look for repeating words
2. identify speakers
3. Are there any triggers

## 3 workstreams
1. Input & Analysis - Kevin/Vik
2. Interpretation - Paloma
3. Display - Pratha/Mark

---

## Setup

### Prerequisites
- Python 3.13.0 (see `api/.python-version`; use [pyenv](https://github.com/pyenv/pyenv) or [mise](https://mise.jdx.dev) to auto-switch)
- A GCP project with billing enabled
- APIs to enable (run once per project):
  ```bash
  gcloud services enable aiplatform.googleapis.com sqladmin.googleapis.com storage.googleapis.com run.googleapis.com
  ```

### Install Google Cloud SDK

**macOS**
```bash
brew install --cask google-cloud-sdk
```

**Windows**

Download and run the installer from https://cloud.google.com/sdk/docs/install#windows, or with winget:
```powershell
winget install Google.CloudSDK
```

**Linux (Debian/Ubuntu)**
```bash
curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg
echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee /etc/apt/sources.list.d/google-cloud-sdk.list
sudo apt-get update && sudo apt-get install google-cloud-cli
```

**Linux (RPM-based: RHEL/Fedora)**
```bash
sudo tee -a /etc/yum.repos.d/google-cloud-sdk.repo << EOM
[google-cloud-cli]
name=Google Cloud CLI
baseurl=https://packages.cloud.google.com/yum/repos/cloud-sdk-el9-x86_64
enabled=1
gpgcheck=1
repo_gpgcheck=0
gpgkey=https://packages.cloud.google.com/yum/doc/rpm-package-key.gpg
EOM
sudo dnf install google-cloud-cli
```

### Authenticate and configure
```bash
gcloud init                        # log in + set default project/region
gcloud auth application-default login   # allow local code to call GCP APIs
```

### Run the API locally
```bash
cd api
pip install -r requirements.txt
uvicorn main:app --reload
```

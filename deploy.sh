#!/usr/bin/env bash
# Deploys the ABA Session Analyzer API to Cloud Run.
# Access is restricted: callers must hold roles/run.invoker on this service.
# Cloud SQL uses private IP only; Cloud Run connects via Serverless VPC Access.
# Run with: bash deploy.sh
set -euo pipefail

# ── Configuration ────────────────────────────────────────────────────────────
PROJECT_ID="qwiklabs-gcp-01-c8623a386b8c"
REGION="us-central1"
SERVICE_NAME="aba-session-analyzer"
IMAGE="gcr.io/${PROJECT_ID}/${SERVICE_NAME}"
SA_NAME="aba-analyzer-sa"
SA_EMAIL="${SA_NAME}@${PROJECT_ID}.iam.gserviceaccount.com"
SQL_INSTANCE="aba-analyzer-db"
SQL_CONN="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
DB_NAME="aba_sessions"
DB_USER="aba_user"
SECRET_NAME="aba-database-url"
VPC_NETWORK="default"
VPC_RANGE_NAME="google-managed-services-${VPC_NETWORK}"
VPC_CONNECTOR_NAME="aba-connector"

echo "=== [1/9] Enabling required GCP APIs ==="
gcloud services enable \
  run.googleapis.com \
  sqladmin.googleapis.com \
  cloudbuild.googleapis.com \
  aiplatform.googleapis.com \
  secretmanager.googleapis.com \
  servicenetworking.googleapis.com \
  vpcaccess.googleapis.com \
  --project="${PROJECT_ID}"

echo ""
echo "=== [2/9] Setting up private services access for Cloud SQL ==="
if ! gcloud compute addresses describe "${VPC_RANGE_NAME}" --global --project="${PROJECT_ID}" &>/dev/null; then
  gcloud compute addresses create "${VPC_RANGE_NAME}" \
    --global \
    --purpose=VPC_PEERING \
    --prefix-length=16 \
    --network="${VPC_NETWORK}" \
    --project="${PROJECT_ID}"
else
  echo "IP range ${VPC_RANGE_NAME} already exists, skipping."
fi

if ! gcloud services vpc-peerings list \
  --network="${VPC_NETWORK}" \
  --project="${PROJECT_ID}" 2>/dev/null | grep -q servicenetworking; then
  gcloud services vpc-peerings connect \
    --service=servicenetworking.googleapis.com \
    --ranges="${VPC_RANGE_NAME}" \
    --network="${VPC_NETWORK}" \
    --project="${PROJECT_ID}"
else
  echo "VPC peering for servicenetworking already exists, skipping."
fi

echo ""
echo "=== [3/9] Creating Cloud SQL Postgres instance with private IP (takes ~5 min on first run) ==="
if ! gcloud sql instances describe "${SQL_INSTANCE}" --project="${PROJECT_ID}" &>/dev/null; then
  gcloud sql instances create "${SQL_INSTANCE}" \
    --database-version=POSTGRES_15 \
    --tier=db-f1-micro \
    --region="${REGION}" \
    --network="projects/${PROJECT_ID}/global/networks/${VPC_NETWORK}" \
    --no-assign-ip \
    --project="${PROJECT_ID}"
else
  echo "Instance ${SQL_INSTANCE} already exists, skipping."
fi

gcloud sql databases create "${DB_NAME}" \
  --instance="${SQL_INSTANCE}" \
  --project="${PROJECT_ID}" 2>/dev/null || echo "Database ${DB_NAME} already exists, skipping."

DB_PASSWORD=$(python3 -c "import secrets; print(secrets.token_urlsafe(24))")
gcloud sql users create "${DB_USER}" \
  --instance="${SQL_INSTANCE}" \
  --password="${DB_PASSWORD}" \
  --project="${PROJECT_ID}" 2>/dev/null || \
  gcloud sql users set-password "${DB_USER}" \
    --instance="${SQL_INSTANCE}" \
    --password="${DB_PASSWORD}" \
    --project="${PROJECT_ID}"

echo ""
echo "=== [4/9] Storing DATABASE_URL in Secret Manager ==="
DATABASE_URL="postgresql+asyncpg://${DB_USER}:${DB_PASSWORD}@/${DB_NAME}?host=/cloudsql/${SQL_CONN}"
if gcloud secrets describe "${SECRET_NAME}" --project="${PROJECT_ID}" &>/dev/null; then
  printf '%s' "${DATABASE_URL}" | \
    gcloud secrets versions add "${SECRET_NAME}" --data-file=- --project="${PROJECT_ID}"
else
  printf '%s' "${DATABASE_URL}" | \
    gcloud secrets create "${SECRET_NAME}" --data-file=- --project="${PROJECT_ID}"
fi

echo ""
echo "=== [5/9] Creating service account ==="
if ! gcloud iam service-accounts describe "${SA_EMAIL}" --project="${PROJECT_ID}" &>/dev/null; then
  gcloud iam service-accounts create "${SA_NAME}" \
    --display-name="ABA Analyzer Cloud Run SA" \
    --project="${PROJECT_ID}"
else
  echo "Service account ${SA_EMAIL} already exists, skipping."
fi

echo ""
echo "=== [6/9] Granting IAM roles to service account ==="
gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${SA_EMAIL}" \
  --role="roles/aiplatform.user" \
  --condition=None

gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${SA_EMAIL}" \
  --role="roles/cloudsql.client" \
  --condition=None

gcloud secrets add-iam-policy-binding "${SECRET_NAME}" \
  --member="serviceAccount:${SA_EMAIL}" \
  --role="roles/secretmanager.secretAccessor" \
  --project="${PROJECT_ID}"

echo ""
echo "=== [7/9] Creating Serverless VPC Access connector ==="
if ! gcloud compute networks vpc-access connectors describe "${VPC_CONNECTOR_NAME}" \
  --region="${REGION}" --project="${PROJECT_ID}" &>/dev/null; then
  gcloud compute networks vpc-access connectors create "${VPC_CONNECTOR_NAME}" \
    --region="${REGION}" \
    --network="${VPC_NETWORK}" \
    --range="10.8.0.0/28" \
    --project="${PROJECT_ID}"
else
  echo "VPC connector ${VPC_CONNECTOR_NAME} already exists, skipping."
fi

echo ""
echo "=== [8/9] Building and pushing Docker image via Cloud Build ==="
gcloud builds submit ./api \
  --tag="${IMAGE}" \
  --project="${PROJECT_ID}"

echo ""
echo "=== [9/9] Deploying to Cloud Run (no public access) ==="
gcloud run deploy "${SERVICE_NAME}" \
  --image="${IMAGE}" \
  --region="${REGION}" \
  --service-account="${SA_EMAIL}" \
  --add-cloudsql-instances="${SQL_CONN}" \
  --vpc-connector="${VPC_CONNECTOR_NAME}" \
  --vpc-egress=private-ranges-only \
  --set-env-vars="GOOGLE_CLOUD_PROJECT=${PROJECT_ID},GCP_LOCATION=${REGION}" \
  --set-secrets="DATABASE_URL=${SECRET_NAME}:latest" \
  --no-allow-unauthenticated \
  --memory=1Gi \
  --cpu=1 \
  --timeout=300 \
  --project="${PROJECT_ID}"

SERVICE_URL=$(gcloud run services describe "${SERVICE_NAME}" \
  --region="${REGION}" \
  --project="${PROJECT_ID}" \
  --format="value(status.url)")

echo ""
echo "=========================================="
echo " Deployment complete!"
echo " Service URL: ${SERVICE_URL}"
echo "=========================================="
echo ""
echo "To grant a project member access to invoke the API:"
echo "  gcloud run services add-iam-policy-binding ${SERVICE_NAME} \\"
echo "    --region=${REGION} \\"
echo "    --member=user:EMAIL@DOMAIN.COM \\"
echo "    --role=roles/run.invoker \\"
echo "    --project=${PROJECT_ID}"
echo ""
echo "To call the API as an authorized user:"
echo "  TOKEN=\$(gcloud auth print-identity-token)"
echo "  curl -H \"Authorization: Bearer \$TOKEN\" ${SERVICE_URL}/health"

#!/usr/bin/env bash
set -euo pipefail

BASE_HTTP="http://localhost:5002"
BASE_HTTPS="https://localhost:7011"
# prefer HTTPS for protected endpoints if your server has it, -k disables cert verification for local dev
CURL="curl -k -sS"

echo "=== Quick health & swagger checks ==="
echo "HTTP /health:"
$CURL -i "$BASE_HTTP/health" || true
echo
echo "HTTPS /swagger (dev only):"
$CURL -i "$BASE_HTTPS/swagger" || true
echo
echo "=== Auth: register & login ==="
EMAIL="sanity+$(date +%s)@example.com"
PASSWORD="Password123"
REGISTER_RESP=$($CURL -X POST "$BASE_HTTPS/api/Auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"fullName\":\"Sanity Tester\"}" || true)
echo "register response:"
echo "$REGISTER_RESP" | jq . || echo "$REGISTER_RESP"
echo

LOGIN_RESP=$($CURL -X POST "$BASE_HTTPS/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
TOKEN=$(echo "$LOGIN_RESP" | jq -r .token)
if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "LOGIN FAILED: response:"
  echo "$LOGIN_RESP"
  exit 1
fi
echo "login ok, token (truncated): ${TOKEN:0:40}..."
echo

AUTH_HEADER="Authorization: Bearer $TOKEN"

echo "=== Projects ==="
echo "GET /api/Projects (should be 200 and return array):"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects" | jq .
echo

echo "Create a project"
CREATE_PROJECT_RESP=$($CURL -X POST "$BASE_HTTPS/api/Projects" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d '{"title":"Scheduler Test Project","description":"temp created by script"}')
echo "$CREATE_PROJECT_RESP" | jq .
PROJECT_ID=$(echo "$CREATE_PROJECT_RESP" | jq -r .id)
echo "PROJECT_ID=$PROJECT_ID"
echo

echo "GET project detail:"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID" | jq .
echo

echo "=== Tasks ==="
echo "Create a task under project"
CREATE_TASK_RESP=$($CURL -X POST "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d '{"title":"First task via script","dueDate":null}')
echo "$CREATE_TASK_RESP" | jq .
TASK_ID=$(echo "$CREATE_TASK_RESP" | jq -r .id)
echo "TASK_ID=$TASK_ID"
echo

echo "List tasks"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks" | jq .
echo

echo "Get single task"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID" | jq .
echo

echo "Update (rename) task and mark completed"
$CURL -i -X PUT "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d '{"title":"First task via script - UPDATED","dueDate":null,"isCompleted":true}'
echo "PUT returned (204 expected) above"
echo

echo "Verify task updated:"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID" | jq .
echo

echo "Toggle completion back to false via PUT using returned title"
CURR=$($CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID")
CURR_TITLE=$(echo "$CURR" | jq -r .title)
$CURL -i -X PUT "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d "{\"title\":$(jq -Rn --arg v "$CURR_TITLE" '$v'),\"dueDate\":null,\"isCompleted\":false}"
echo "Toggle done"
echo

echo "Delete task"
$CURL -i -X DELETE "$BASE_HTTPS/api/Projects/$PROJECT_ID/Tasks/$TASK_ID" -H "$AUTH_HEADER"
echo "Deleted task (204 expected above)"
echo

echo "=== Project update & delete ==="
echo "Update project title"
$CURL -i -X PUT "$BASE_HTTPS/api/Projects/$PROJECT_ID" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d '{"title":"Scheduler Test Project - UPDATED","description":"updated by script"}'
echo "GET project now:"
$CURL -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID" | jq .
echo

echo "Delete project"
$CURL -i -X DELETE "$BASE_HTTPS/api/Projects/$PROJECT_ID" -H "$AUTH_HEADER"
echo "GET project (should be 404):"
$CURL -i -H "$AUTH_HEADER" "$BASE_HTTPS/api/Projects/$PROJECT_ID" || true
echo

echo "=== Scheduler endpoint ==="
# create a project for scheduler test
SCHED_PROJECT=$($CURL -X POST "$BASE_HTTPS/api/Projects" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  -d '{"title":"SchedulerRunner","description":"for scheduler test"}')
SCHED_ID=$(echo "$SCHED_PROJECT" | jq -r .id)
echo "Scheduler project id: $SCHED_ID"

# use payload.json file in current dir
if [ ! -f payload.json ]; then
  echo "payload.json not found — creating a default one temporarily"
  cat > /tmp/payload.json <<'JSON'
{
  "tasks": [
    { "title": "Design API", "estimatedHours": 5, "dueDate": "2025-10-25", "dependencies": [] },
    { "title": "Implement Backend", "estimatedHours": 12, "dueDate": "2025-10-28", "dependencies": ["Design API"] },
    { "title": "Build Frontend", "estimatedHours": 10, "dueDate": "2025-10-30", "dependencies": ["Design API"] },
    { "title": "E2E Test", "estimatedHours": 8, "dueDate": "2025-10-31", "dependencies": ["Implement Backend","Build Frontend"] }
  ]
}
JSON
  PAYLOAD="/tmp/payload.json"
else
  PAYLOAD="payload.json"
fi

echo "POSTing payload to /api/Projects/$SCHED_ID/Schedule"
SCHED_RESP=$($CURL -X POST "$BASE_HTTPS/api/Projects/$SCHED_ID/Schedule" -H "$AUTH_HEADER" -H "Content-Type: application/json" \
  --data-binary @"$PAYLOAD" || true)
echo "scheduler response:"
echo "$SCHED_RESP" | jq . || echo "$SCHED_RESP"
echo

echo "=== Done tests ==="

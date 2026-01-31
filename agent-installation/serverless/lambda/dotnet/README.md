## .NET 8 Lambda sample

This folder contains a minimal two-function Lambda chain:

- `CallerFunction` invokes `WorkerFunction` via the AWS Lambda SDK.
- `WorkerFunction` returns a simple payload to keep validation easy.

### Prerequisites

- AWS SAM CLI
- .NET SDK 8.x
- AWS credentials configured for deployment

### Build and run

```bash
cd agent-installation/serverless/lambda/dotnet
sam build
sam deploy --guided
```

### GitHub Actions deployment

The workflow uses these repository secrets:

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `SAM_STACK_NAME`
- `SAM_S3_BUCKET`

### Invoke the caller

```bash
aws lambda invoke \
  --function-name dd-prospects-caller \
  --payload '{"message":"prospects","count":3}' \
  response.json
cat response.json
```

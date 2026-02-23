

# Kubernetes Agent Installation

- *I recommend using **Helm** instead of Operator*.
- Official Datadog Document: https://docs.datadoghq.com/containers/kubernetes/installation/?tab=helm#installation

## Configuration File

- you may use the `datadog-values.yaml` file that is provided in the repo, but be sure to replace all relevant environment variables with your own

## Creating Datadog API Key as a Kubernetes Secret
Recommendation:
- Install the datadog agent in its own `datadog` namespace. SSI will not work on deployments in the same namespace as the agent.

```bash
kubectl create namespace datadog
kubectl create secret generic datadog-secret -n datadog --from-literal api-key=<DATADOG_API_KEY>
```

## Installation

```bash
helm repo add datadog https://helm.datadoghq.com
helm repo update
```
- deploy the agent using the provided `datadog-values.yaml` file
```bash
helm install datadog-agent datadog/datadog -n datadog -f datadog-values.yaml
```
- deploying changes to the agent
```bash
helm upgrade datadog-agent datadog/datadog -n datadog -f datadog-values.yaml
```
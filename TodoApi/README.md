# Todo API — Docker & Kubernetes Learning Lab

A minimal .NET 8 REST API with Swagger UI, used as a hands-on project to learn
containerisation and orchestration step by step.

**Stack:** .NET 8 · ASP.NET Core Web API · Swagger (Swashbuckle)

---

## Table of Contents

1. [Run the API locally](#1-run-the-api-locally)
2. [Simple Dockerfile](#2-simple-dockerfile)
3. [Multi-stage Dockerfile](#3-multi-stage-dockerfile)
4. [Kubernetes Deployment & Service](#4-kubernetes-deployment--service)
5. [ConfigMap & Secret](#5-configmap--secret)

---

## 1. Run the API Locally

```bash
cd TodoApi
dotnet run
```

Open your browser at `http://localhost:5000` — Swagger UI loads at the root path.

| Endpoint         | Method | Description        |
|------------------|--------|--------------------|
| `/api/todo`      | GET    | List all todos     |
| `/api/todo/{id}` | GET    | Get one todo by ID |
| `/api/todo`      | POST   | Create a todo      |
| `/api/todo/{id}` | PUT    | Update a todo      |
| `/api/todo/{id}` | DELETE | Delete a todo      |

---

## 2. Simple Dockerfile

A **Dockerfile** is a text recipe that tells Docker how to package your app into
a self-contained **image**. Anyone who has Docker installed can run that image
without installing .NET.

### How it works — concept

```
Your code  ──►  docker build  ──►  Image  ──►  docker run  ──►  Container
```

- **Image** — a frozen snapshot of your app + runtime (think: a ZIP file).
- **Container** — a running instance of that image (think: unzipped, running process).

### Create `Dockerfile`

Place this file next to `TodoApi.csproj`:

```dockerfile
# Base image: official .NET 8 runtime (lighter than the full SDK image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory inside the container
WORKDIR /app

# Copy everything from your machine into /app inside the image
COPY . .

# Restore NuGet packages and publish a Release build into /app/publish
RUN dotnet publish -c Release -o /app/publish

# Tell Docker which port the app listens on (documentation only)
EXPOSE 80

# Command that runs when the container starts
ENTRYPOINT ["dotnet", "/app/publish/TodoApi.dll"]
```

> **Why is this "simple"?**  
> The SDK (compiler) and the final runtime live in the same image, making it
> large (~800 MB). Fine for learning; multi-stage (next section) fixes this.

### Build & run

```bash
# Build the image and tag it "todo-api:simple"
docker build -t todo-api:simple .

# Run a container — map host port 8080 → container port 80
docker run -p 8080:80 todo-api:simple
```

Open `http://localhost:8080` — Swagger UI should appear.

### Useful Docker commands

```bash
docker images                  # list all images on your machine
docker ps                      # list running containers
docker ps -a                   # all containers (including stopped)
docker stop <container-id>     # stop a running container
docker rm <container-id>       # remove a stopped container
docker rmi todo-api:simple     # delete the image
```

---

## 3. Multi-stage Dockerfile

The simple Dockerfile bundles the **SDK** (needed only during build) into the
final image. Multi-stage builds fix this: one stage compiles, the next copies
only the compiled output into a tiny runtime-only image.

```
Stage 1 (build)  ──►  dotnet publish  ──►  /app/publish/
                                                  │
Stage 2 (final)  ◄── COPY --from=build ───────────┘
```

### Benefits

| | Simple | Multi-stage |
|---|---|---|
| Image size | ~800 MB | ~200 MB |
| SDK in final image? | Yes | No |
| Layer caching (fast rebuilds) | Partial | Yes |

### Replace `Dockerfile` with the multi-stage version

```dockerfile
# ══════════════════════════════════════════════════════════════════
# STAGE 1 — Build
# Use the full SDK image to compile and publish the app
# ══════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy only the .csproj first, then restore.
# Docker caches this layer — if .csproj hasn't changed, restore is skipped.
COPY ["TodoApi.csproj", "."]
RUN dotnet restore

# Copy remaining source files and publish a Release build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ══════════════════════════════════════════════════════════════════
# STAGE 2 — Final (runtime only)
# Start fresh from the small runtime image — no SDK, no source code
# ══════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy only the published output from the build stage
COPY --from=build /app/publish .

# ASP.NET Core 8 containers listen on 8080 by default
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
```

### Add `.dockerignore`

Prevents unnecessary files from being sent to Docker — speeds up the build:

```
bin/
obj/
*.user
.vs/
.git/
README.md
```

### Build & run

```bash
docker build -t todo-api:v1 .
docker run -p 8080:8080 todo-api:v1
```

---

## 4. Kubernetes Deployment & Service

**Kubernetes (K8s)** manages containers across machines. Think of it as a
supervisor that keeps your app running, restarts crashes, and load-balances
traffic across multiple copies.

### Key concepts

| Concept | Plain English |
|---|---|
| **Pod** | Smallest unit — wraps one container |
| **Deployment** | Declares *how many* pods to run and *which image* to use |
| **Service** | Stable network address that routes traffic to pods |
| **Node** | A physical/virtual machine in the cluster |

```
Other pods / Ingress ──► ClusterIP Service ──► Pod 1 (todo-api)
                                          ──► Pod 2 (todo-api)
                                          (load-balanced inside the cluster)
```

> We use **ClusterIP** (the default service type) — the API is reachable
> only from within the cluster. In a real setup you'd place an Ingress or
> API Gateway in front to expose it externally.

### Prerequisites

- A running cluster — local options: [Docker Desktop K8s](https://docs.docker.com/desktop/kubernetes/) or [minikube](https://minikube.sigs.k8s.io/)
- `kubectl` CLI installed
- Your image pushed to a registry

```bash
# Tag and push to Docker Hub (replace "yourname")
docker tag todo-api:v1 yourname/todo-api:v1
docker push yourname/todo-api:v1
```

### `k8s/deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: todo-api
  labels:
    app: todo-api
spec:
  replicas: 2                         # run 2 pods (high availability)
  selector:
    matchLabels:
      app: todo-api
  template:
    metadata:
      labels:
        app: todo-api                 # Service uses this label to find pods
    spec:
      containers:
        - name: todo-api
          image: yourname/todo-api:v1 # ← replace with your image
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
          resources:
            requests:                 # minimum resources guaranteed
              cpu: "100m"             # 100 millicores = 0.1 CPU core
              memory: "128Mi"
            limits:                   # hard maximum — pod is killed if exceeded
              cpu: "500m"
              memory: "256Mi"
          readinessProbe:             # K8s only routes traffic once this passes
            httpGet:
              path: /api/todo
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
```

### `k8s/service.yaml`

```yaml
apiVersion: v1
kind: Service
metadata:
  name: todo-api-service
spec:
  selector:
    app: todo-api                     # routes traffic to pods with this label
  ports:
    - protocol: TCP
      port: 80                        # port other pods use to call this service
      targetPort: 8080                # port the container actually listens on
  type: ClusterIP                     # reachable only inside the cluster (default)
```

> **Service types quick-reference:**  
> `ClusterIP` — internal only ✓ (we use this) · `NodePort` — exposed on each node's IP · `LoadBalancer` — cloud public IP

### Apply & verify

```bash
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

kubectl get pods -w                        # watch pods start up (Ctrl-C to stop)
kubectl get service todo-api-service       # confirm ClusterIP is assigned
```

To test inside the cluster, run a temporary pod:

```bash
# Spin up a curl pod and call the service by its DNS name
kubectl run curl-test --image=curlimages/curl --rm -it --restart=Never \
  -- curl http://todo-api-service/api/todo
```

### Handy kubectl commands

```bash
kubectl describe pod <pod-name>              # full details + events (great for debugging)
kubectl logs <pod-name>                      # stdout from a pod
kubectl exec -it <pod-name> -- /bin/sh       # shell into a running pod

kubectl scale deployment todo-api --replicas=5          # scale to 5 pods
kubectl rollout restart deployment todo-api             # rolling restart (zero downtime)
kubectl set image deployment/todo-api todo-api=yourname/todo-api:v2  # deploy new version
kubectl rollout status deployment/todo-api              # watch the rollout

kubectl delete -f k8s/                       # remove everything
```

---

## 5. ConfigMap & Secret

Your app needs configuration that changes per environment (log level, feature
flags, connection strings, API keys). Hardcoding these in the image is bad
practice — Kubernetes provides two dedicated resources:

| Resource | Use for | Stored as |
|---|---|---|
| **ConfigMap** | Non-sensitive config (env name, log level, URLs) | Plain text |
| **Secret** | Sensitive values (passwords, API keys, tokens) | Base64-encoded |

> **Important:** Base64 is *encoding*, not *encryption*. Secrets are only as
> secure as your cluster's RBAC. For production, pair Secrets with tools like
> HashiCorp Vault or Azure Key Vault.

### `k8s/configmap.yaml`

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: todo-api-config
data:
  # Plain key-value pairs
  ASPNETCORE_ENVIRONMENT: "Production"
  App__LogLevel: "Information"
  App__AllowSwagger: "true"
```

### `k8s/secret.yaml`

Values inside a Secret **must be base64-encoded**.

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: todo-api-secret
type: Opaque
data:
  # echo -n "my-db-password" | base64  →  bXktZGItcGFzc3dvcmQ=
  ConnectionStrings__DefaultConnection: bXktZGItcGFzc3dvcmQ=
  # echo -n "super-secret-key" | base64  →  c3VwZXItc2VjcmV0LWtleQ==
  ApiKey: c3VwZXItc2VjcmV0LWtleQ==
```

> **How to encode a value:**
> ```bash
> # Linux / macOS
> echo -n "my-db-password" | base64
>
> # PowerShell (Windows)
> [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("my-db-password"))
> ```

### Wire them into the Deployment

Update the `env:` block in `k8s/deployment.yaml` to reference the ConfigMap
and Secret instead of hardcoding values:

```yaml
          env:
            # ── From ConfigMap ────────────────────────────────────────────
            - name: ASPNETCORE_ENVIRONMENT
              valueFrom:
                configMapKeyRef:
                  name: todo-api-config        # ConfigMap name
                  key: ASPNETCORE_ENVIRONMENT  # key inside the ConfigMap

            - name: App__LogLevel
              valueFrom:
                configMapKeyRef:
                  name: todo-api-config
                  key: App__LogLevel

            # ── From Secret ───────────────────────────────────────────────
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: todo-api-secret        # Secret name
                  key: ConnectionStrings__DefaultConnection

            - name: ApiKey
              valueFrom:
                secretKeyRef:
                  name: todo-api-secret
                  key: ApiKey
```

> **Shortcut — inject the entire ConfigMap/Secret at once:**
> ```yaml
>           envFrom:
>             - configMapRef:
>                 name: todo-api-config    # every key becomes an env var
>             - secretRef:
>                 name: todo-api-secret
> ```

### Apply & verify

```bash
# Always apply ConfigMap and Secret BEFORE the Deployment
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

# Confirm the env vars are visible inside a running pod
kubectl exec -it <pod-name> -- printenv | grep App__
kubectl exec -it <pod-name> -- printenv | grep ApiKey
```

### Inspect & decode from the cluster

```bash
kubectl get configmap todo-api-config -o yaml    # view ConfigMap
kubectl get secret todo-api-secret -o yaml       # view Secret (base64 values)

# Decode a specific secret value
kubectl get secret todo-api-secret \
  -o jsonpath="{.data.ApiKey}" | base64 --decode
```

---

## Project Structure

```
TodoApi/
├── Controllers/
│   └── TodoController.cs        # REST endpoints (GET, POST, PUT, DELETE)
├── Models/
│   └── Todo.cs                  # Todo data model
├── k8s/
│   ├── configmap.yaml           # Non-sensitive configuration
│   ├── secret.yaml              # Sensitive values (base64-encoded)
│   ├── deployment.yaml          # Kubernetes Deployment (2 replicas)
│   └── service.yaml             # Kubernetes Service (ClusterIP)
├── Program.cs                   # App bootstrap + Swagger config
├── TodoApi.csproj
├── Dockerfile                   # Multi-stage build
├── .dockerignore
└── README.md                    # ← you are here
```

---

> **Recommended learning path:**
> Local run → Simple Dockerfile → Multi-stage Dockerfile → K8s Deployment + Service → ConfigMap & Secret

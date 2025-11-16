# New-Service.ps1 — README

## Purpose
`New-Service.ps1` scaffolds a new service from a template by copying files and replacing placeholders in both file contents and names. It can write into an existing output directory (`-OutputDir`) or create a new one under `-TargetRoot`.

---

## Requirements
- Windows with PowerShell 7+
- `robocopy` available in `PATH` (standard on Windows)
- Read access to the template directory
- If you pass `-OutputDir`, that directory must already exist

---

## Template Placeholders
The script replaces these placeholders everywhere (file contents, file names, directory names):

- `${ServiceName}` → PascalCase name as provided (e.g., `SubmissionService`)
- `${service-name}` → kebab-case from `ServiceName` (e.g., `submission-service`)
- `${app-name}` → kebab-case from `ServiceName` (e.g., `submission-service`)
- `${app-base-path}` → first PascalCase chunk of `ServiceName`, lowercased (e.g., `submission`)

Left intact on purpose (not replaced): tokens like `#{replicaNo}#`, `#{container-registry}#`, and other `#{...}#`.

---

## Naming Rules
Given `ServiceName`:

1) **`${ServiceName}`**
   - Use as-is (PascalCase).
   - Examples: `SubmissionService`, `PlaylistService`, `TrackInferenceApi`, `HTTPGatewayService`, `ETL2Service`.

2) **`${app-name}`** (kebab-case from `ServiceName`)
   - `SubmissionService` → `submission-service`
   - `TrackInferenceApi` → `track-inference-api`
   - `HTTPGatewayService` → `http-gateway-service`
   - `ETL2Service` → `etl2-service`

3) **`${app-base-path}`** (first PascalCase chunk, lowercased)
   - `SubmissionService` → `submission`
   - `TrackInferenceApi` → `track`
   - `HTTPGatewayService` → `http`
   - `ETL2Service` → `etl2`

Whitespace in `ServiceName` is removed before processing.

---

## What Gets Edited vs. Skipped

**Edited (text files):**
`.cs, .csproj, .sln, .json, .jsonc, .yml, .yaml, .md, .txt, .xml, .config, .props, .targets, .ts, .tsx, .js, .jsx, .scss, .css, .ps1, .psm1, .sh, .dockerfile, .env, .editorconfig, .tf, .tfvars, .ini, .cmd, .bat, .yarnrc, .npmrc`
Plus special names without extensions: `Dockerfile`, `.env*`, `.gitignore`, `.gitattributes`.

**Skipped (treated as binary):**
Everything else (images, archives, binaries, etc.).

---

## Copy and Rename Behavior
1) Copy template → destination (uses `robocopy` to minimize long-path issues).
2) Replace placeholders in text files.
3) Rename directories (deepest-first) and then files if their names contain placeholders.

---

## Typical Mappings (Examples)

### Kubernetes Manifests (`manifests/`)
```yaml
# Service
metadata:
  name: ${app-name}
selector:
  app: ${app-name}

# Ingress
- path: /${app-base-path}

# Deployment
metadata:
  name: ${app-name}
spec:
  template:
    metadata:
      labels:
        app: ${app-name}
    spec:
      containers:
      - name: ${app-name}
```

### Dockerfile
```dockerfile
ENTRYPOINT ["dotnet", "${ServiceName}.dll"]
```

### C# Source
```csharp
namespace ${ServiceName}.Routes;
```

### Pipeline YAML
```yaml
variables:
- name: "imageRepo"
  value: '${ServiceName}'
```

### Project/Solution
- `${ServiceName}.csproj`, `tests/${ServiceName}.Test.csproj`, and `.sln` entries are renamed accordingly.

---

## Usage

Generate into an existing output folder (will not create it):
```
powershell -ExecutionPolicy Bypass -File .\scripts\New-Service.ps1 -TemplatePath "microservices\Template" -TargetRoot "microservices" -ServiceName "SubmissionService" -OutputDir "microservices\spred.api.submission"
```

Generate into a new folder named after `ServiceName` under `TargetRoot`:
```
powershell -ExecutionPolicy Bypass -File .\scripts\New-Service.ps1 -TemplatePath "microservices\Template" -TargetRoot "microservices" -ServiceName "SubmissionService"
```

Override names explicitly:
```
powershell -ExecutionPolicy Bypass -File .\scripts\New-Service.ps1 -TemplatePath "microservices\Template" -TargetRoot "microservices" -ServiceName "TrackInferenceApi" -AppName "custom-app" -AppBasePath "track"
```

Dry run (logs only, no writes):
```
powershell -ExecutionPolicy Bypass -File .\scripts\New-Service.ps1 -TemplatePath "microservices\Template" -TargetRoot "microservices" -ServiceName "SubmissionService" -DryRun
```

---

## Exit Codes and Errors
- `Template not found` — invalid `-TemplatePath`.
- `Target already exists` — target directory already present when not using `-OutputDir`.
- `robocopy failed with code > 7` — copy failure; inspect console for details.

---

## Troubleshooting

**Incorrect kebab-case (`app-name`)**
Ensure `ServiceName` is valid PascalCase (no special characters). Whitespace is removed before conversion.

**Incorrect `${app-base-path}`**
The first PascalCase chunk is taken and lowercased. If you need a different value, pass `-AppBasePath yourvalue`.

**Long paths**
If renames still fail, move the repo closer to drive root (e.g., `D:\\src\\…`).

---

## Post-Generation Checklist
- Replace remaining `#{...}#` tokens via CI/CD or variable groups.
- Validate Kubernetes manifests (namespace, secrets, `imagePullSecrets`, ingress class).
- Confirm Dockerfile `ARG`/environment variables (feeds, access tokens).
- Restore/build the solution and run tests.
- Verify package feeds and credentials.

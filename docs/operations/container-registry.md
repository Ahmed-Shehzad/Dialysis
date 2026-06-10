# Container registry — publishing the deployment images (JFrog Artifactory / any OCI registry)

The committed Helm charts (`deploy/charts/dialysis-<env>/`) reference every application image
through a values parameter (e.g. `parameters.ehr_api.ehr_api_image`, default `ehr-api:latest`),
and the committed compose folders build images locally. Neither says where images are *hosted* —
that is this page: how to build the full image set (6 module APIs, 8 BFFs, the gateway, 7 SPAs =
22 images) and push it to a registry the cluster can pull from.

Everything is registry-agnostic; JFrog specifics are called out where they matter.

## One-time registry setup (JFrog)

- **Free tier**: [JFrog Container Registry (JCR)](https://jfrog.com/container-registry/) supports
  Docker + Helm repositories at no cost (self-hosted install:
  <https://docs.jfrog.com/installation/docs/docker>). A full **Artifactory Pro** instance adds
  NuGet/npm proxying and Xray scanning, but is not required for this flow.
- Create a **local Docker repository** (e.g. key `dialysis-docker`). The registry prefix is then
  `<server>/dialysis-docker`, e.g. `mycompany.jfrog.io/dialysis-docker`.
- Authenticate the Docker client with an **identity token** (Artifactory → user profile →
  Generate Identity Token):

  ```bash
  docker login mycompany.jfrog.io   # username + identity token
  ```

## Build + push: `./build.sh PushImages`

```bash
./build.sh PushImages --registry mycompany.jfrog.io/dialysis-docker [--environment prod] [--version 1.4.0]
```

What it does:

1. Publishes a **scratch** compose project (under `.nuke/temp/`, never committed) with
   `DIALYSIS_IMAGE_REGISTRY`/`DIALYSIS_IMAGE_TAG` set, so every repo-built service gets an
   `image: <registry>/<service>:<tag>` name next to its `build:` stanza
   (`ContainerRegistryPublishExtensions` + `ComposePublishExtensions.ApplyPublishedImageName`).
   The tag defaults to the GitVersion-derived SemVer; `--version` overrides it.
2. Runs `docker compose build`, then pushes each of the 22 images.
3. Writes `artifacts/images/values-images-<env>.yaml` — a Helm values override mapping every
   chart image parameter to the pushed reference.

Deploy the committed chart against the pushed images:

```bash
helm install dialysis deploy/charts/dialysis-prod \
  -n dialysis-prod --create-namespace \
  -f artifacts/images/values-images-prod.yaml
```

For a **private** registry, give the namespace pull credentials cluster-side (no chart change
needed — the charts stay registry-free by design):

```bash
kubectl -n dialysis-prod create secret docker-registry dialysis-registry-credentials \
  --docker-server=mycompany.jfrog.io --docker-username=<user> --docker-password=<identity-token>
kubectl -n dialysis-prod patch serviceaccount default \
  -p '{"imagePullSecrets":[{"name":"dialysis-registry-credentials"}]}'
```

Compose-based deployments can pull instead of build the same way: point the scratch compose's
host at the registry (`docker compose -f <scratch>/docker-compose.yaml pull && up -d`), or keep
using the committed `deploy/compose/<env>/` folders, which build locally.

## Why the committed artifacts never name a registry

`deploy-artifacts.yml` regenerates `deploy/compose/` and `deploy/charts/` from the AppHost with a
**bare environment** and fails the PR on drift. `DIALYSIS_IMAGE_REGISTRY` is therefore strictly a
publish-time input: set it (or use `PushImages`, which sets it for you) and the qualified names
appear in the *scratch* output; never commit artifacts generated with it, or the drift gate will
flag them.

## CI sketch

A release workflow step (after `docker login` with `${{ secrets.JFROG_USER }}` /
`${{ secrets.JFROG_IDENTITY_TOKEN }}`):

```yaml
- run: ./build.sh PushImages --registry ${{ vars.JFROG_REGISTRY }}/dialysis-docker --environment prod
- uses: actions/upload-artifact@v4
  with: { name: helm-image-values, path: artifacts/images/values-images-prod.yaml }
```

GitVersion gives every push a unique, traceable tag (`1.4.1-ci.3` on trunk, `1.4.0` on a release
tag), so images are immutable per build and `latest` is never relied upon.

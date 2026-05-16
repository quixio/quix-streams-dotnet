SHELL := /usr/bin/env bash

DOTNET_IMAGE ?= quix-streams-dotnet-ci:8.0
DOTNET_DOCKERFILE := builds/docker/dotnet-ci.Dockerfile
SOLUTION := src/Quix.Streams.sln
CONFIGURATION ?= Release
VERSION ?= 0.8.0.0
PACKAGE_VERSION ?= $(VERSION)
INFORMATIONAL_VERSION ?= $(PACKAGE_VERSION)
TEST_HANG_TIMEOUT ?= 2m
PACKAGE_OUTPUT := builds/csharp/nuget/build-result
TEST_RESULTS := test-results
PACK_PROJECTS := \
	src/QuixStreams.Streaming/QuixStreams.Streaming.csproj \
	src/QuixStreams.Telemetry/QuixStreams.Telemetry.csproj \
	src/QuixStreams.Kafka/QuixStreams.Kafka.csproj \
	src/QuixStreams.Kafka.Transport/QuixStreams.Kafka.Transport.csproj

DOCKER_HOST ?=
DOCKER_SOCKET := $(wildcard /var/run/docker.sock)
DOCKER_SOCKET_ARGS := $(if $(DOCKER_HOST),,$(if $(DOCKER_SOCKET),-v /var/run/docker.sock:/var/run/docker.sock,))
DOCKER_SOCKET_GID := $(shell if [ -z "$(DOCKER_HOST)" ] && [ -S /var/run/docker.sock ]; then stat -c '%g' /var/run/docker.sock 2>/dev/null || stat -f '%g' /var/run/docker.sock 2>/dev/null; fi)
DOCKER_GROUP_ARGS := $(if $(DOCKER_SOCKET_GID),--group-add $(DOCKER_SOCKET_GID),)
DOCKER_HOST_ARGS := $(if $(DOCKER_HOST),-e DOCKER_HOST=$(DOCKER_HOST),)
USER_ARGS := $(shell id -u):$(shell id -g)
DOCKER_RUN := docker run --rm \
	--user $(USER_ARGS) \
	$(DOCKER_GROUP_ARGS) \
	-e HOME=/tmp \
	-e NUGET_PACKAGES=/tmp/nuget/packages \
	-e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
	-e DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
	-e DOTNET_NOLOGO=1 \
	-e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
	$(DOCKER_HOST_ARGS) \
	$(DOCKER_SOCKET_ARGS) \
	-v "$(CURDIR):/work" \
	-w /work \
	$(DOTNET_IMAGE)
DOCKER_TEST_RUN := docker run --rm \
	--user $(USER_ARGS) \
	$(DOCKER_GROUP_ARGS) \
	--network host \
	-e HOME=/tmp \
	-e NUGET_PACKAGES=/tmp/nuget/packages \
	-e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
	-e DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
	-e DOTNET_NOLOGO=1 \
	-e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
	$(DOCKER_HOST_ARGS) \
	$(DOCKER_SOCKET_ARGS) \
	-v "$(CURDIR):/work" \
	-w /work \
	$(DOTNET_IMAGE)

.PHONY: help build-image build test package restore clean shell

help:
	@echo "Targets:"
	@echo "  make build-image          Build the Docker image used by all other targets"
	@echo "  make restore              Restore packages in Docker"
	@echo "  make build                Build the solution in Docker"
	@echo "  make test                 Run tests in Docker"
	@echo "  make package              Build NuGet packages in Docker"
	@echo "  make clean                Remove generated bin/obj/package output"
	@echo ""
	@echo "Variables:"
	@echo "  DOTNET_IMAGE=$(DOTNET_IMAGE)"
	@echo "  CONFIGURATION=$(CONFIGURATION)"
	@echo "  VERSION=$(VERSION)"
	@echo "  PACKAGE_VERSION=$(PACKAGE_VERSION)"
	@echo "  INFORMATIONAL_VERSION=$(INFORMATIONAL_VERSION)"
	@echo "  TEST_HANG_TIMEOUT=$(TEST_HANG_TIMEOUT)"
	@echo "  DOCKER_HOST=$(DOCKER_HOST)"

build-image:
	docker build -f $(DOTNET_DOCKERFILE) -t $(DOTNET_IMAGE) .

restore: build-image
	$(DOCKER_RUN) dotnet restore $(SOLUTION) --source https://api.nuget.org/v3/index.json

build: build-image
	$(DOCKER_RUN) bash -lc 'set -euo pipefail; dotnet restore $(SOLUTION) --source https://api.nuget.org/v3/index.json; dotnet build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore'

test: build-image
	$(DOCKER_TEST_RUN) bash -lc 'set -euo pipefail; rm -rf "$(TEST_RESULTS)"; mkdir -p "$(TEST_RESULTS)"; dotnet restore $(SOLUTION) --source https://api.nuget.org/v3/index.json; dotnet test $(SOLUTION) --configuration $(CONFIGURATION) --no-restore --results-directory "$(TEST_RESULTS)" --logger "console;verbosity=normal" --logger "trx" --blame --blame-hang-timeout=$(TEST_HANG_TIMEOUT)'

package: build-image
	$(DOCKER_RUN) bash -lc 'set -euo pipefail; dotnet restore $(SOLUTION) --source https://api.nuget.org/v3/index.json; rm -rf "$(PACKAGE_OUTPUT)"; mkdir -p "$(PACKAGE_OUTPUT)"; cp LICENSE src/LICENSE; trap "rm -f src/LICENSE" EXIT; for project in $(PACK_PROJECTS); do dotnet pack "$$project" --configuration $(CONFIGURATION) --no-restore --output "$(PACKAGE_OUTPUT)" /p:Version=$(VERSION) /p:AssemblyVersion=$(VERSION) /p:FileVersion=$(VERSION) /p:InformationalVersion=$(INFORMATIONAL_VERSION) /p:PackageVersion=$(PACKAGE_VERSION); done'

clean:
	find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
	rm -rf "$(PACKAGE_OUTPUT)"
	rm -rf "$(TEST_RESULTS)"
	rm -f src/LICENSE

shell:
	$(DOCKER_RUN) bash

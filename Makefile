SHELL := /bin/bash
.ONESHELL:
.DELETE_ON_ERROR:
.SHELLFLAGS := -eu -o pipefail -c
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules

ifeq ($(OS), Windows_NT)
    DETECTED_OS := Windows
else
    DETECTED_OS := $(shell sh -c 'uname 2>/dev/null || echo Unknown')
endif

NUGET_KEY ?=
NUGET_URL := 'https://api.nuget.org/v3/index.json'

.DEFAULT_GOAL := build

.PHONY: clean
clean:
ifeq ($(DETECTED_OS), Windows)
	if exist .packages del .packages
else
	rm -rf ./.packages
endif
	dotnet clean

.PHONY: restore
restore:
	dotnet restore

.PHONY: build
build:
	dotnet build

.PHONY: test
test:
	dotnet test

.PHONY: example
example:
	dotnet run --project ./examples/MJ.Akka.Projections.EventStoreToRavenDbExample

.PHONY: benchmark
benchmark:
ifeq ($(DETECTED_OS), Windows)
	dotnet run -c Release --project ./benchmarks/MJ.Akka.Projections.Benchmarks
else
	sudo dotnet run -c Release --project ./benchmarks/MJ.Akka.Projections.Benchmarks
endif
	$(MAKE) build

.PHONY: package
package: clean
	dotnet pack ./src/MJ.Akka.Projections -c Release -o ./.packages
	dotnet pack ./src/MJ.Akka.Projections.Cluster.Sharding -c Release -o ./.packages
	dotnet pack ./src/MJ.Akka.Projections.Storage.RavenDb -c Release -o ./.packages
	dotnet pack ./src/MJ.Akka.Projections.Storage.InfluxDb -c Release -o ./.packages

.PHONY: publish
publish: package
ifdef NUGET_URL
ifdef NUGET_KEY
	dotnet nuget push "./.packages/*.nupkg" -k $(NUGET_KEY) -s $(NUGET_URL) --skip-duplicate
endif
endif
.DEFAULT_GOAL := help

SHELL := /bin/bash
.SHELLFLAGS := -euo pipefail -c

SLN := GiviKDev.OAuth.slnx

# ──────────────────────────────────────────────────────────────────────────────
# Terminal colors
# ──────────────────────────────────────────────────────────────────────────────

BOLD   := $(shell tput bold 2>/dev/null || true)
RESET  := $(shell tput sgr0 2>/dev/null || true)
GREEN  := $(shell tput setaf 2 2>/dev/null || true)
YELLOW := $(shell tput setaf 3 2>/dev/null || true)

# ──────────────────────────────────────────────────────────────────────────────
# Setup
# ──────────────────────────────────────────────────────────────────────────────

# --- Setup ---

setup: ## Install pre-commit hooks
	pre-commit install
	pre-commit install --hook-type commit-msg
	@echo "$(GREEN)Git hooks installed.$(RESET)"

update-hooks: ## Update pre-commit hooks to latest versions
	pre-commit autoupdate
	@echo "$(GREEN)Pre-commit hooks updated. Review changes in .pre-commit-config.yaml$(RESET)"

# ──────────────────────────────────────────────────────────────────────────────
# .NET
# ──────────────────────────────────────────────────────────────────────────────

# --- .NET ---

dotnet-build: ## Build the solution
	dotnet build $(SLN) --warnaserrors

dotnet-rebuild: ## Clean and rebuild
	dotnet build $(SLN) --warnaserrors --no-incremental

dotnet-test: ## Run all tests
	dotnet test $(SLN) --no-build

dotnet-format: ## Run dotnet format (auto-fix)
	dotnet format $(SLN)

dotnet-format-check: ## Verify formatting without changes
	dotnet format $(SLN) --verify-no-changes

dotnet-update: ## Update all NuGet packages
	dotnet outdated $(SLN) --upgrade

dotnet-outdated: ## Check for outdated NuGet packages (read-only)
	dotnet list $(SLN) package --outdated

dotnet-upgrade-sdk: ## Upgrade global.json to latest installed .NET SDK
	@dotnet new globaljson --roll-forward latestFeature --force
	@echo "$(GREEN)Updated global.json to SDK $$(dotnet --version)$(RESET)"

# ──────────────────────────────────────────────────────────────────────────────
# Quality
# ──────────────────────────────────────────────────────────────────────────────

# --- Quality ---

ci: dotnet-format-check dotnet-build dotnet-test ## Run all CI checks
	@echo "$(GREEN)All CI checks passed.$(RESET)"

pre-commit: ## Run all pre-commit hooks + rebuild + test
	pre-commit run --all-files
	@$(MAKE) dotnet-rebuild
	@$(MAKE) dotnet-test
	@echo "$(GREEN)Pre-commit checks passed.$(RESET)"

# ──────────────────────────────────────────────────────────────────────────────
# Release
# ──────────────────────────────────────────────────────────────────────────────

# --- Release ---

release-dry: ## Preview next release (dry run)
	npx semantic-release --dry-run

release: ## Run semantic-release (CI only)
	npx semantic-release

# ──────────────────────────────────────────────────────────────────────────────
# Help
# ──────────────────────────────────────────────────────────────────────────────

.PHONY: help setup update-hooks dotnet-build dotnet-rebuild dotnet-test dotnet-format \
        dotnet-format-check dotnet-update dotnet-outdated dotnet-upgrade-sdk \
        ci pre-commit release-dry release

help: ## Show available targets
	@echo ''
	@echo '$(BOLD)GiviKDev.OAuth — Task Runner$(RESET)'
	@echo ''
	@echo 'Usage: make [target]'
	@echo ''
	@awk 'BEGIN {FS = ":.*?## "; section=""} \
		/^# --- / { gsub(/^# --- | ---$$/, "", $$0); section=$$0; \
			printf "\n$(BOLD)%s$(RESET)\n", section; next } \
		/^[a-zA-Z_-]+:.*?## / { printf "  $(GREEN)%-28s$(RESET) %s\n", $$1, $$2 }' \
		$(MAKEFILE_LIST)
	@echo ''

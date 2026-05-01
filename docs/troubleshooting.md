# Troubleshooting SimplicityTools

SimplicityTools is designed to work with zero config on your first run. If something isn't working as expected, this guide covers the most common issues and how to fix them.

---

## Installation & PATH

### "`dotnet-simplicity` command not found" after `dotnet tool install`

**Symptom:** You ran `dotnet tool install --global SimplicityTools.Cli` but the command is not available in a new terminal.

**Cause:** The global tools directory is not in your system `PATH`.

**Solutions:**

1. **Verify the tool is installed:**
   ```bash
   dotnet tool list --global | grep SimplicityTools.Cli
   ```
   
   If it appears in the list, the installation succeeded; the problem is PATH discovery.

2. **Find the tools directory:**
   ```bash
   # On macOS/Linux:
   echo $DOTNET_ROOT/tools
   # or typically: ~/.dotnet/tools
   
   # On Windows:
   # %USERPROFILE%\.dotnet\tools
   ```

3. **Add to PATH:**
   
   **On macOS/Linux**, add to your shell profile (`~/.bash_profile`, `~/.zshrc`, etc.):
   ```bash
   export PATH="$HOME/.dotnet/tools:$PATH"
   ```
   
   Then reload the shell:
   ```bash
   source ~/.zshrc  # or ~/.bash_profile for bash
   ```
   
   **On Windows (PowerShell)**:
   ```powershell
   $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
   ```
   
   To make it permanent, use the GUI: System Properties → Environment Variables → Edit `PATH`.

4. **Test the command:**
   ```bash
   dotnet-simplicity --version
   ```

---

## .NET SDK

### "Could not execute because the application was not found" or "The following error occurred"

**Symptom:** You run `dotnet simplicity analyze...` and get an error about a missing .NET runtime or SDK.

**Cause:** Your system is missing .NET 10 SDK (or the target version that matches the package). SimplicityTools ships as a compiled tool targeting `net10.0`.

**Solution:**

1. **Check your current .NET version:**
   ```bash
   dotnet --version
   ```

2. **Install the required version:**
   - Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
   - macOS: `brew install dotnet` (or download the installer)
   - Linux: Use your package manager or download from Microsoft
   - Windows: Download the installer

3. **Verify after install:**
   ```bash
   dotnet --version
   dotnet --list-sdks
   ```

4. **If you're building from source**, use the `.NET version matrix`

---

## Roslyn Analyzers

### "Analyzer not showing in IDE" or "SF00XX diagnostics are not appearing"

**Symptom:** You added `SimplicityTools.Analyzers` to your project, but the IDE is not showing SF00XX warnings.

**Cause:** The most common reasons are:
- The project hasn't rebuilt since adding the package
- The IDE cache is stale
- The analyzer is disabled in your project settings

**Solutions:**

1. **Clean and rebuild the project:**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Restart your IDE** (Visual Studio, Rider, VS Code):
   - Close and reopen the IDE completely
   - Or reload the workspace/solution

3. **Verify the package was added with `PrivateAssets="all"`:**
   
   Open your `.csproj` file and confirm:
   ```xml
   <ItemGroup>
     <PackageReference Include="SimplicityTools.Analyzers" Version="X.Y.Z" PrivateAssets="all" />
   </ItemGroup>
   ```
   
   The `PrivateAssets="all"` attribute is required. Without it, analyzers don't load correctly.

4. **Check your IDE analyzer settings:**
   
   **Visual Studio:**
   - Tools → Options → Text Editor → C# → Advanced → "Enable full solution analyzer"
   - Set to `true`
   
   **Rider:**
   - Settings → Tools → Resharper → Inspections → "Run inspections for solution"
   - Ensure enabled
   
   **VS Code:**
   - Ensure the C# or Omnisharp extension is installed and running
   - Reload the window

5. **Verify the analyzer is working from the command line:**
   ```bash
   dotnet build --no-incremental
   ```
   
   If no SF00XX warnings appear here either, the package may not be in the restore graph. Check `dotnet package search SimplicityTools.Analyzers`.

---

## Report Generation

### "Report generation failed" or "Failed to write to output directory"

**Symptom:** Running `dotnet simplicity report` produces an error about file I/O.

**Cause:** Usually one of:
- Insufficient disk space
- Read-only permissions on the output directory
- The `simplicity-report/` directory is locked by another process
- Invalid characters in the solution path

**Solutions:**

1. **Verify disk space:**
   ```bash
   # macOS/Linux:
   df -h .
   
   # Windows (PowerShell):
   Get-Volume
   ```

2. **Check permissions:**
   
   Ensure you can write to the current directory:
   ```bash
   # macOS/Linux:
   touch test-write.txt
   rm test-write.txt
   
   # Windows (PowerShell):
   New-Item -Path . -Name "test-write.txt" -Force
   Remove-Item "test-write.txt"
   ```
   
   If the test fails, you need write permissions. Ask your administrator or use `sudo` (not recommended for tools).

3. **Close other processes using the report directory:**
   
   If you have the HTML report open in a browser, close it before regenerating:
   ```bash
   # macOS: killall Safari (or Chrome, Firefox, etc.)
   # Linux: killall firefox (or your browser)
   # Windows: Close the browser window manually
   ```

4. **Remove stale report artifacts:**
   ```bash
   rm -rf simplicity-report
   dotnet simplicity report path/to/Solution.sln
   ```

5. **Ensure the solution path is valid:**
   
   The tool cannot analyze solutions with spaces or special characters in the path. If your solution path contains spaces, quote it:
   ```bash
   dotnet simplicity report "path/to/My Solution.sln"
   ```

---

## CI/CD Integration

### "Command works locally but fails in CI"

**Symptom:** Your CI/CD job runs `dotnet simplicity` but the build fails on the agent.

**Causes vary by platform, but common issues are:**
- .NET SDK not installed on the CI agent
- Global tool not installed on the CI agent
- Working directory differs between local and agent
- Baseline or history files not committed to the repo

**Solutions by platform:**

#### GitHub Actions

See [CI/CD Integration guide](using-the-simplicity-tools.md#cicd-integration) for full examples.

Quick checklist:
```yaml
- name: Install .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'

- name: Install global tool
  run: dotnet tool install --global SimplicityTools.Cli

- name: Ensure tool is in PATH
  run: echo "$HOME/.dotnet/tools" >> $GITHUB_PATH

- name: Run analysis
  run: dotnet simplicity analyze YourSolution.sln
```

**Key points:**
- `actions/setup-dotnet` installs the SDK (not always pre-installed)
- Add `~/.dotnet/tools` to `$GITHUB_PATH` explicitly
- Use absolute or repo-relative paths for solutions

#### Azure Pipelines

See [CI/CD Integration guide](using-the-simplicity-tools.md#cicd-integration) for full examples.

Quick checklist:
```yaml
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- script: dotnet tool install --global SimplicityTools.Cli
  displayName: Install SimplicityTools

- script: echo "##vso[task.prependpath]$HOME/.dotnet/tools"
  displayName: Add tool to PATH

- script: dotnet simplicity analyze $(Build.SourcesDirectory)/YourSolution.sln
  displayName: Run complexity analysis
```

**Key points:**
- `UseDotNet@2` ensures the SDK is available
- Use `$(Build.SourcesDirectory)` for repo paths
- Add tools to PATH using `##vso[task.prependpath]`

#### GitLab CI

See [CI/CD Integration guide](using-the-simplicity-tools.md#cicd-integration) for full examples.

Quick checklist:
```yaml
stages:
  - analyze

complexity:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  stage: analyze
  script:
    - dotnet tool install --global SimplicityTools.Cli
    - export PATH="$HOME/.dotnet/tools:$PATH"
    - dotnet simplicity analyze YourSolution.sln
```

**Key points:**
- Use an official Microsoft .NET image with the SDK pre-installed
- Install the global tool in the job
- Export the tool path before use

### "Baseline was not found" in CI

**Symptom:** Running `dotnet simplicity diff --fail-on-regression` fails with "Baseline file was not found".

**Cause:** The baseline file (`.simplicity-baseline.json`) exists locally but is not committed to the repository, or the job is running in a temporary workspace that doesn't have it.

**Solution:**

1. **Create a baseline locally:**
   ```bash
   dotnet simplicity baseline path/to/YourSolution.sln
   ```
   
   This generates `.simplicity-baseline.json` in the current directory.

2. **Commit the baseline to git:**
   ```bash
   git add .simplicity-baseline.json
   git commit -m "Add complexity baseline"
   ```

3. **Verify in CI:**
   
   Your CI job should now find the file. Run a test build to confirm:
   ```bash
   dotnet simplicity diff path/to/YourSolution.sln --fail-on-regression
   ```

---

## Analyzer Build Cleanup (Advanced)

### "Analyzer keeps using a stale version after updating the package"

**Symptom:** You updated `SimplicityTools.Analyzers` to a newer version, but the IDE still shows old diagnostics.

**Cause:** The Roslyn analyzer host caches compiled assemblies. Simply updating the package reference doesn't clear the cache.

**Solution:**

1. **Clean all build artifacts:**
   ```bash
   dotnet clean
   rm -rf bin obj
   ```

2. **Clear IDE-specific caches:**
   
   **Visual Studio:**
   ```bash
   # Windows:
   rmdir "%LocalAppData%\Microsoft\VisualStudio\*\ComponentModelCache" /s
   ```
   
   **Rider:**
   ```bash
   # macOS:
   rm -rf ~/Library/Caches/JetBrains/Rider*/
   
   # Linux:
   rm -rf ~/.cache/JetBrains/Rider*/
   ```
   
   **VS Code (Omnisharp):**
   ```bash
   # macOS:
   rm -rf ~/Library/Application\ Support/omnisharp-vscode/
   
   # Linux:
   rm -rf ~/.omnisharp-vscode/
   ```

3. **Restart the IDE** and rebuild the solution.

---

## Advanced Diagnostics

### "I want to see what the CLI is doing"

**Diagnostic mode:**
```bash
# Most CLI commands accept a verbose flag (if implemented in your version):
dotnet simplicity analyze path/to/Solution.sln --verbose
```

**From source:**

If you're building from the source repository and want diagnostic output:
```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --configuration Debug --verbosity detailed
```

### "Configuration validation is failing but I don't know why"

**Symptom:** Running the CLI produces an error about `simplicity.json` but the error message is unclear.

**Solution:**

1. **Validate schema manually:**
   ```bash
   # Check against docs/simplicity-schema.json
   # Key requirements:
   # - All numeric values must be numbers (not strings)
   # - passingScore must be between 0 and 1
   # - team size must be > 0
   ```

2. **Print the config the CLI is using:**
   ```bash
   # Create a minimal test config:
   cat > simplicity.json << 'EOF'
   {
     "filters": {
       "primaryPathRatioTarget": 0.6
     }
   }
   EOF
   
   # Run the CLI and check the error message
   dotnet simplicity analyze YourSolution.sln
   ```

3. **Start from a known-good template:**
   ```json
   {
     "tca": {
       "teamSize": 8,
       "averageEngineerMonthlySalaryUsd": 15000,
       "estimatedMonthlyIncidentCount": 4,
       "onCallHourlyRateUsd": 150,
       "attritionCoefficientPercent": 15
     },
     "filters": {
       "primaryPathRatioTarget": 0.6,
       "prematureAbstractionRatioTarget": 0.25,
       "maxMethodComplexity": 5,
       "maxOnboardingHours": 40,
       "passingScore": 0.7
     }
   }
   ```

---

## Still stuck?

If you've tried the above and the issue persists:

1. **Check the project README:** [SimplicityTools on GitHub](https://github.com/cwoodruff/SimplicityTools#readme)
2. **File an issue:** [GitHub Issues](https://github.com/cwoodruff/SimplicityTools/issues)
   - Include the command you ran
   - Include the error message (all of it)
   - Include your OS and .NET version (`dotnet --version`)
3. **Try the sample solutions:** Run the CLI against `samples/Sample.Simplified` to verify the tool works at all

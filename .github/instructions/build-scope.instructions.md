---
applyTo: "**/*.cs"
---
When building to verify C# changes, **never build the full solution** (.sln, .slnx or .slnf). Always build only the specific project(s) that were modified. Use `msbuild /t:build <ProjectPath>` and target the individual .csproj file.
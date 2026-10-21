# VeloxPdf

VeloxPdf is a .NET library for generating PDFs.

**Author:** Momentive Software

## Running the samples

Prerequisite: the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or later (check with `dotnet --version`).

```powershell
# 1. Clone the repo and go to its root
git clone https://github.com/sharadchavare1995/VeloxPdf.git
cd VeloxPdf

# 2. Build everything
dotnet build VeloxPdf.sln

# 3. (Optional) Run the tests
dotnet test

# 4. Run the sample console app
cd samples\VeloxPdf.Samples
dotnet run
```

Notes:

- Run `dotnet run` from `samples\VeloxPdf.Samples`. The repo root only has the `.sln` file, so `dotnet run` there fails with "Couldn't find a project to run". From the root you can use `dotnet run --project samples\VeloxPdf.Samples` instead.
- The generated PDFs are written to an `output` folder in the directory you ran the command from, for example `samples\VeloxPdf.Samples\output`.
- The build prints many `CS1591` (missing XML comment) warnings. They are harmless.

See [`nuget/README.md`](nuget/README.md) for the full API guide and more examples.

---
# Use the “landing” layout so we get a nice full-width headline.
_layout: landing
---

# Icarus Drone Service Documentation

Welcome to the official documentation for **Icarus Drone Service**, the WPF application that manages drone-servicing jobs (Regular vs. Express), queues them, processes them, and tracks finished jobs. This site is split into two main sections:

1. **Docs** (this Markdown-based documentation)  
2. **API** (auto-generated from the C# source code via DocFX)

You can use the navigation pane on the left to switch between these two sections:

- **Docs** → Detailed how-to guides, architectural overview, examples, and diagrams.  
- **API** → Complete, per-class reference for every public type and member (i.e. your `IcarusDrone` window class, model classes in `Models/`, etc.).

---

## Quick Start

### 1. Prerequisites

- [.NET 8.0 SDK (or later)](https://dotnet.microsoft.com/download) installed  
- [DocFX](https://dotnet.github.io/docfx/) CLI installed (e.g. via Chocolatey: `choco install docfx` or `scoop install docfx`)  
- Visual Studio 2022 (or 2019) if you plan to edit/build the WPF application itself  

> **Tip:** Make sure your WPF project is fully built (and that “XML documentation file” is enabled under Project → Properties → Build) so that DocFX can read your `<summary>…</summary>` XML comments at metadata time.

---

### 2. Project Layout

Below is the relevant folder structure. Note that **`docfx.json`**, **`toc.yml`**, and **`index.md`** all live in the root of this repository. Your actual C# project (`IcarusDroneServiceApp.csproj`) is inside the `IcarusDroneServiceApp/` subfolder.

IcarusDroneServiceApp\ ← (Root of documentation repository)
│
├─ docfx.json ← DocFX configuration (already set up)
├─ toc.yml ← Table of contents (links to Docs + API)
├─ index.md ← THIS FILE (your Markdown “homepage”)
│
├─ IcarusDroneServiceApp\ ← (Your WPF app)
│ ├─ IcarusDroneServiceApp.csproj
│ ├─ App.xaml
│ ├─ IcarusDrone.xaml
│ ├─ IcarusDrone.xaml.cs
│ └─ Models
│ ├─ Drone.cs
│ └─ ServiceRepository.cs
│
└─ docs\ (optional) ← Extra Markdown articles or images
├─ articles
│ └─ architecture.md
└─ images\

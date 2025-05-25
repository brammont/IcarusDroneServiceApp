# Icarus Drone Service Application

> A WPF desktop app demonstrating queue-based processing of drone service jobs with full UI, data structures, tracing, testing and CITEMS-compliant code.

---

## Table of Contents

1. [Project Overview](#project-overview)  
2. [Features](#features)  
3. [Prerequisites](#prerequisites)  
4. [Installation & Setup](#installation--setup)  
5. [Project Structure](#project-structure)  
6. [UML & Data Structures](#uml--data-structures)  
7. [Programming Criteria Checklist](#programming-criteria-checklist)  
8. [Running the App](#running-the-app)  
9. [Trace Logging & Debugging](#trace-logging--debugging)  
10. [UI Testing Report](#ui-testing-report)  
11. [Coding Standards & Conventions](#coding-standards--conventions)  
12. [Contributing](#contributing)  
13. [License](#license)  

---

## Project Overview

The **Icarus Drone Service Application** is a .NET WPF desktop application for managing drone service jobs. It uses:

- **`Queue<Drone>`** for Regular and Express service queues  
- **`List<Drone>`** for completed jobs  
- A rich UI with numeric up/down, ListView, ListBox, radio buttons, multi-line textboxes  
- Full tracing via `System.Diagnostics.Trace`  
- Manual UI test cases and automated unit tests (xUnit)  
- UML diagrams for class and data structures  
- CITEMS-compliant coding standards  

---

## Features

- **New Job Entry**:  
  - Client name, drone model, service tag (100–900 step 10)  
  - Priority: Regular or Express (Express adds 15% surcharge)  
  - Cost field with **two-decimal validation**  
  - Problem description (multi-line)  
- **Queues**:  
  - View pending jobs in **Regular** and **Express** tabs  
- **Processing**:  
  - Buttons to dequeue and move to Finished list  
- **Finished Jobs**:  
  - ListBox showing “Client – $Cost”  
  - Double-click to remove finished job  
- **Trace Logging**:  
  - Live in VS Output window  
  - Written to `trace.log`  
- **Testing**:  
  - 6 manual UI test cases with screen-capture placeholders  
  - Automated xUnit tests (Drone class, queue logic, surcharge, validation)  
- **Design**:  
  - UML Class diagram for `Drone`  
  - Data structure diagram for `ServiceRepository`  

---

## Prerequisites

- Windows 10+  
- [.NET 8 SDK](https://dotnet.microsoft.com/download)  
- Visual Studio 2022 (or newer) with **.NET Desktop Development** workload  
- [Extended.Wpf.Toolkit](https://www.nuget.org/packages/Extended.Wpf.Toolkit/) for `<xctk:IntegerUpDown>`  

---

## Installation & Setup

1. **Clone the repo**  
   ```bash
   git clone https://github.com/yourusername/IcarusDroneServiceApp.git
   cd IcarusDroneServiceApp
2. **Restore NuGet packages**

    dotnet restore

3. **Open in Visual Studio**

        File → Open → Project/Solution → IcarusDroneServiceApp.sln

4. **Install Extended WPF Toolkit**

        NuGet → Browse → Extended.Wpf.Toolkit → Install

## Project Structure

IcarusDroneServiceApp.sln
└── IcarusDroneServiceApp
    ├── IcarusDroneServiceApp.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── IcarusDrone.xaml
    ├── IcarusDrone.xaml.cs
    ├── Models
    │   ├── Drone.cs
    │   └── ServiceRepository.cs
    ├── Properties
    │   └── AssemblyInfo.cs
    └── trace.log             # runtime trace output

##UML & Data Structures
*Drone Class Diagram (PlantUML)*

@startuml
class Drone {
  - clientName       : String
  - droneModel       : String
  - serviceProblem   : String
  - cost             : Decimal
  - serviceTag       : Integer

  + ClientName       : String
  + DroneModel       : String
  + ServiceProblem   : String
  + Cost             : Decimal
  + ServiceTag       : Integer
  + Display()        : String
}
@enduml

*ServiceRepository Diagram (PlantUML)*

@startuml
class ServiceRepository {
  + Queue<Drone> RegularService
  + Queue<Drone> ExpressService
  + List<Drone> FinishedList
}
ServiceRepository "1" --> "*" Drone : RegularService
ServiceRepository "1" --> "*" Drone : ExpressService
ServiceRepository "1" --> "*" Drone : FinishedList
@enduml

##Programming Criteria Checklist

Done	Criterion	Part Done (Code / Location)
☐	ServiceTag numeric control (100–900, step 10)	<xctk:IntegerUpDown Name="numTag" … />
☐	Priority radio buttons (GroupName="Priority": Regular & Express)	rbRegular, rbExpress in XAML
☐	Multi-line ServiceProblem textbox	txtProblem with AcceptsReturn="True"
☐	ListView showing Tag, ClientName, DroneModel, ServiceProblem, Cost	SetupListViewColumns() in IcarusDrone.xaml.cs
☐	ListBox showing finished jobs (Client – $Cost)	RefreshFinished() binding to lbFinished
☐	Drone.cs with private fields, public getters/setters, Display(), title/sentence casing	Models/Drone.cs
☐	List<Drone> FinishedList declared	Field in IcarusDrone.xaml.cs
☐	Queue<Drone> RegularService, ExpressService declared	Fields in IcarusDrone.xaml.cs
☐	AddNewItem() method—reads inputs, enqueues based on priority	IcarusDrone.xaml.cs:AddNewItem
☐	15% surcharge on Express	if(priority=="Express") cost*=1.15;
☐	GetServicePriority() returns “Regular”/“Express”	Method in code-behind
☐	RefreshQueues() displays RegularService in lvRegular	Method in code-behind
☐	RefreshQueues() displays ExpressService in lvExpress	Method in code-behind
☐	TxtCost_PreviewTextInput() enforces up to 2-decimal input	Handler in code-behind
☐	IncrementTag() increments ServiceTag	Method in code-behind
☐	LvRegular_SelectionChanged populates fields from selected Drone	Handler in code-behind
☐	LvExpress_SelectionChanged populates fields from selected Drone	Handler in code-behind
☐	ProcessReg_Click dequeues RegularService, adds to FinishedList	Handler in code-behind
☐	ProcessExpr_Click dequeues ExpressService, adds to FinishedList	Handler in code-behind
☐	OnRemoveFinished double-click removes from FinishedList	Handler in code-behind
☐	ClearForm() clears textboxes and resets priority selection	Method in code-behind
☐	XML comments and CITEMS style mapping above each method	Throughout IcarusDrone.xaml.cs

##Running the App

    Start Debugging in Visual Studio (F5).

    Use the UI: add jobs, switch tabs, process and finish jobs.

    Check Output Window (Debug → Windows → Output) for trace logs.

    Inspect trace.log in the application folder for a full trace.

##Trace Logging & Debugging

    Configured in App.xaml.cs via DefaultTraceListener and TextWriterTraceListener("trace.log").

    Key methods instrumented with Trace.TraceInformation / Trace.TraceWarning.

    Set breakpoints in AddNewItem, ProcessReg_Click, etc., inspect variables in Locals window.

##UI Testing Report

**Follow Question 7 template for 6 manual test cases:**

    Add Regular Job

    Add Express Job & surcharge

    Invalid cost validation

    Process Regular Job

    Process Express Job

    Remove Finished Job
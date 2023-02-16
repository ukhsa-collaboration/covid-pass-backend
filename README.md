# Covid Pass Back End

The NHS COVID Pass Back End is an API the Covid Pass Front End is utilising. It has integrations with NHSD where medical exemptions and test results are fetched from. The NHS Covid Pass Back End also generates 2D Barcodes for the NHS Covid Pass Front End as well as defining the rules for generating the aforementioned 2D Barcodes. It conducts processes based on a users' actions within the [NHS Covid Pass Front End](https://github.com/nhsx/covid-pass-web) such as sending out emails. 

## Prerequisites

### **Install GIT**

GIT is used for version/source control.

GIT can be installed from here: [https://git-scm.com/](https://git-scm.com/)

You will be prompted to answer a series of questions when installing GIT on Windows. You should select the following two configuration options where possible (otherwise use the default selected):

- Checkout as-is, commit as-is.
- Git Credential Manager for Windows

You will also need to install:
- .NET 6.0 / C# 10 (i.e. [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Rider 2022.2](https://blog.jetbrains.com/dotnet/2022/08/02/rider-2022-2-released/)+)

### **How to get started**

To clone the repository:

1. Open a new CMD instance (if installed use git bash) and navigate to the destination directory where you'd like the cloned repository to exist. If necessary, create this folder first.
2. Execute the command: git clone https://github.com/nhsx/covid-pass-backend.git

**Link to the licence file**

https://github.com/nhsx/covid-pass-backend/blob/main/LICENSE

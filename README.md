# Fleet Management System – ODOT Portfolio Project

<!-- 🏷️ Progress & Status Badges -->

![Sprint](https://img.shields.io/badge/Sprint-4--Day%20Plan-blueviolet)
![Status](https://img.shields.io/badge/Current%20Status-Day%202%20--%20Monitoring%20Integration-orange)
![Framework](https://img.shields.io/badge/.NET-8.0-blue)
![Frontend](https://img.shields.io/badge/Frontend-Vanilla%20JS%20%2F%20HTML-lightgrey)
![Logging](https://img.shields.io/badge/Logging-Serilog-success)
![Database](https://img.shields.io/badge/Database-SQL%20Server-lightblue)
![License](https://img.shields.io/badge/License-MIT-lightgrey)

---

## 🧭 Overview

This Fleet Management System is a production-ready ASP.NET Core application designed to demonstrate **operations, monitoring, and system administration** skills relevant to the ODOT Operations & Policy Analyst role.

It includes:

- Full CRUD API for managing vehicles and maintenance records
- Integrated logging via **Serilog**
- SQL Server backend with seeded data
- Monitoring via **Splunk** and **Azure Application Insights**
- CI/CD automation through **Azure DevOps Pipelines**
- Documentation and diagnostic SQL queries for real-world observability scenarios

---

## 🏁 Sprint Progress Overview

| Day       | Focus Area             | Key Goals                                                       | Status         |
| --------- | ---------------------- | --------------------------------------------------------------- | -------------- |
| **Day 1** | Core App & CRUD        | ASP.NET Core setup, EF migrations, Serilog logging, seeded data | ✅ Complete    |
| **Day 2** | Monitoring Integration | Splunk + App Insights telemetry, SQL diagnostics                | ✅ Complete    |
| **Day 3** | DevOps & Operations    | Azure Pipelines, CI/CD, environment configs, runbooks           | 🟠 In Progress |
| **Day 4** | Documentation & Demo   | README, screenshots, demo video, job fair materials             | ⏳ Pending     |

---

## 📊 Deployment & Monitoring Status

| Component                | Description                         | Status         |
| ------------------------ | ----------------------------------- | -------------- |
| **API**                  | Deployed to Azure App Service       | 🟢 Live (Dev)  |
| **Logging (Serilog)**    | Console + File + Splunk integration | 🟢 Active      |
| **Application Insights** | Telemetry / traces connected        | 🟢 Active      |
| **SQL Diagnostics**      | Custom admin endpoint / DMV queries | 🟢 Implemented |
| **CI/CD Pipeline**       | Azure DevOps YAML build + deploy    | ⏳ Pending     |
| **Demo Video**           | 5-minute overview                   | ⏳ Pending     |

---

> 🧭 **Next Milestone:** Finish/verify app running on Azure, polish repo, and make doc/demo video
>
> DONE:Complete monitoring integrations (Splunk + App Insights) and commit progress by end of Day 2.

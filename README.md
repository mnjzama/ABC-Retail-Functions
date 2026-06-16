# 🛒 ABC Retail Cloud Modernization Platform
---

## 📖 Overview

ABC Retail is a rapidly growing online retailer that faced significant challenges with its legacy on-premises infrastructure. As customer demand increased during peak shopping seasons such as Christmas and Black Friday, the existing systems struggled to maintain performance, reliability, and scalability.

This project modernizes ABC Retail's order processing ecosystem using **Microsoft Azure Cloud Services**, **Azure Functions**, **Azure Storage**, and **SQL Server**, creating a highly scalable, event-driven architecture capable of supporting future business growth.

---

## 🎯 Problem Statement

ABC Retail's legacy infrastructure suffered from:

* ❌ Slow and inefficient order processing
* ❌ Limited scalability during peak shopping periods
* ❌ Delays in message delivery
* ❌ Processing failures caused by outdated middleware
* ❌ Poor product image storage management
* ❌ Difficulties generating real-time business insights
* ❌ Increased operational costs and customer dissatisfaction

These challenges negatively impacted customer experience and reduced business efficiency.

---

## 🚀 Solution

This solution leverages Microsoft Azure's cloud-native services to create a modern, scalable, and reliable retail platform.

### Key Improvements

✅ Serverless Order Processing using Azure Functions

✅ Reliable Message Queuing with Azure Queue Storage

✅ Scalable Product Image Storage using Azure Blob Storage

✅ Structured Data Management through Azure SQL Database

✅ Event-Driven Architecture for real-time processing

✅ Improved Fault Tolerance and Reliability

✅ Reduced Infrastructure Maintenance

✅ Enhanced Scalability During Peak Demand

---

# 🏗️ System Architecture

```text
Customer Places Order
          │
          ▼
    Azure Function
          │
          ▼
   Azure Queue Storage
          │
          ▼
 Queue Trigger Function
          │
          ▼
 Azure SQL Database
          │
          ▼
 Business Reporting

Product Images
          │
          ▼
 Azure Blob Storage
```

---

# ☁️ Azure Services Used

| Service               | Purpose                                  |
| --------------------- | ---------------------------------------- |
| Azure Functions       | Serverless execution of business logic   |
| Azure Queue Storage   | Reliable asynchronous message processing |
| Azure Blob Storage    | Product image storage                    |
| Azure SQL Database    | Persistent relational data storage       |
| Azure Storage Account | Centralized storage management           |
| Azure Portal          | Cloud resource administration            |

---

# 🔧 Technologies Used

### Development

* C#
* ASP.NET Core
* Azure Functions
* .NET 8
* Visual Studio

### Database

* Microsoft SQL Server
* Azure SQL Database

### Cloud Services

* Azure Blob Storage
* Azure Queue Storage
* Azure Storage Accounts

### Version Control

* Git
* GitHub

---

# ⚡ Core Features

## 🛍️ Order Processing

* Create customer orders
* Validate order information
* Queue orders for processing
* Store order records in Azure SQL Database

---

## 📦 Queue-Based Processing

* Decoupled architecture
* Improved reliability
* Retry support
* Scalable message handling

---

## 🖼️ Product Image Management

* Upload images to Azure Blob Storage
* Secure cloud storage
* Fast retrieval and access
* Reduced local storage dependency

---

## 👥 Customer Management

* Customer registration
* Customer information storage
* Order association

---

## 🛒 Product Management

* Product creation
* Product updates
* Inventory information management

---

# 🔒 Security Features

* Azure-managed authentication
* Secure connection strings
* Cloud-based access control
* Data protection through Azure services
* Secure storage account access

---

# 📈 Benefits Achieved

| Before Modernization      | After Modernization       |
| ------------------------- | ------------------------- |
| Legacy infrastructure     | Cloud-native architecture |
| Limited scalability       | Elastic scalability       |
| Slow message processing   | Event-driven processing   |
| Shared network drives     | Blob Storage              |
| High maintenance overhead | Serverless management     |
| Processing bottlenecks    | Automated workflows       |

---

# 🧪 Testing

The solution was tested to ensure:

* ✅ Successful order creation
* ✅ Queue message delivery
* ✅ Queue-trigger execution
* ✅ Blob upload functionality
* ✅ SQL Database persistence
* ✅ Error handling and recovery
* ✅ End-to-end workflow execution

---

# 📚 Prerequisites

Before running the solution, ensure the following are installed:

* Microsoft Visual Studio
* Microsoft SQL Server
* Azure Subscription
* Azure Storage Account
* Azure Functions Core Tools
* Git
* GitHub Account

---

# ⚙️ Configuration

Update the following settings inside:

```json
appsettings.json
```

```json
{
  "AzureWebJobsStorage": "your-storage-connection-string",
  "SqlConnectionString": "your-sql-connection-string",
  "BlobConnectionString": "your-blob-connection-string"
}
```

---

# 🚀 Running the Project

### Clone the Repository

```bash
git clone https://github.com/mnjzama/ABC-Retail.git
```

### Open Solution

```text
Open in Visual Studio
```

### Restore Packages

```bash
dotnet restore
```

### Build Project

```bash
dotnet build
```

### Run Azure Functions

```bash
func start
```

---

# 🎓 Academic Context

This project was developed as part of a Cloud Development and Azure-focused academic assessment, demonstrating practical implementation of:

* Cloud Computing
* Serverless Computing
* Event-Driven Architecture
* Message Queuing
* Cloud Storage
* Database Integration
* Scalable Enterprise Solutions

---

# 👨‍💻 Author

* Name: Mandlenkosi Njabulo Zama
* GitHub: https://github.com/mnjzama

---

# 📄 License

This project is intended for educational and academic purposes.

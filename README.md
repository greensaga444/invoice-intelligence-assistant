# 發票智慧助理

這是一套以 Windows 在地部署為主的桌面解決方案，幫你把發票下載、AI 輔助會計判定、人工覆核分流，以及訓練/治理流程串在一起。

> 內容以實作為準：請優先參考 `src/` 程式碼、`scripts/` 腳本、`config/` 設定範本。

## 專案概述

目前專案由 4 個 WPF 應用程式 + 1 個共用核心函式庫組成：

- **InvoiceLauncher**：整合式主控台，集中設定與模組啟動協調。
- **InvoiceDownloader**：客戶清單匯入、MOF 驗證、批次下載發票至 Folder A。
- **InvoiceAiProcessor**：規則/快取/RAG/本地 LLM 判定管線，輸出到 Folder B / Folder C。
- **InvoiceTrainer**：人工回饋寫回知識庫 + 資料集驗證/訓練/評估/發布流程（UI 操作，無需 CLI）。
- **InvoiceIntelligence.Core**：共用設定、下載器、處理器、訓練器與治理邏輯。

## 儲存庫結構

```text
invoice-intelligence-assistant/
|- src/
|  |- InvoiceLauncher/
|  |- InvoiceDownloader/
|  |- InvoiceAiProcessor/
|  |- InvoiceTrainer/
|  |- InvoiceAppBootstrapper/
|  `- InvoiceIntelligence.Core/
|- tests/InvoiceIntelligence.Core.Tests/
|- scripts/
|- config/
`- docs/
```

## 執行架構

### 端到端流程

1. Downloader 先從 `config/company-list.xlsx` 匯入客戶資料。
2. 接著完成 MOF 認證。
3. Downloader 會依客戶把 `.xlsx` 輸出到 **Folder A**，同時產生批次 `download-log-*.log`。
4. Processor 再讀取 Folder A 的 `.xlsx`，依序執行：
   - 規則引擎
   - 記憶體案例快取
   - 向量 RAG 檢索
   - 本地啟發式 LLM 補判
5. Processor 會輸出：
   - **Folder B**（可直接使用）
   - **Folder C**（低信心/人工覆核）
6. 最後你可以在 Trainer 進行：
   - 將人工核准回饋寫入本地知識庫
   - 模型驗證/訓練/評估/發布

### 全模組流程圖

```mermaid
flowchart TB
    subgraph LA[InvoiceLauncher]
        direction LR
        A[主控台]
        B[設定管理<br/>config/downloader.ini + config/processor.ini]
        C[啟動 Downloader]
        D[啟動 Processor]
        E[啟動 Trainer]
        A --> B
        A --> C
        A --> D
        A --> E
    end

    subgraph DW[InvoiceDownloader]
        direction LR
        G[MOF 驗證]
        H[批次下載]
        I[Folder A<br/>原始發票 .xlsx + download-log]
        G --> H --> I
    end

    subgraph PR[InvoiceAiProcessor]
        direction LR
        J[Rule Engine]
        K[Case Cache]
        L2[Vector RAG]
        M[Local Heuristic LLM]
        N[Folder B<br/>高信心輸出]
        O[Folder C<br/>低信心人工覆核]
        J --> K --> L2 --> M
        M --> N
        M --> O
    end

    subgraph TR[InvoiceTrainer]
        direction LR
        P[手動核准回饋]
        Q[data/knowledge/knowledge-cases.json]
        R[資料集驗證]
        S[模型訓練]
        T[模型評估]
        U[模型發布]
        V[build/training/*]
        P --> Q
        R --> S --> T --> U --> V
    end

    F[輸入資料<br/>config/company-list.xlsx] --> G
    C --> G
    I --> J
    D --> J
    O --> P
    O --> R
    N --> R
    E --> P
    E --> R
```

### 部署執行目錄

發佈腳本會先產生啟動包裝程式，再把實際執行檔移到 `libs/`。

```text
InvoiceIntelligence/
|- InvoiceLauncher.exe
|- libs/
|- downloader/
|  |- InvoiceDownloader.exe
|  `- libs/
|- processor/
|  |- InvoiceAiProcessor.exe
|  `- libs/
|- trainer/
|  |- InvoiceTrainer.exe
|  `- libs/
`- config/
   |- downloader.ini
   `- processor.ini
```

`InvoiceAppBootstrapper` 會負責啟動對應的 `libs\<AppName>.exe`。

## 環境需求

- **作業系統**：Windows（桌面目標是 `net8.0-windows`）。
- **.NET SDK**：
  - 建置與發佈需要 SDK 8+。
  - 跑測試目前需要 SDK 9+（測試專案目標 `net9.0-windows`）。
  - 可直接用以下指令安裝（Windows / winget）：

```powershell
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.DotNet.SDK.9
dotnet --list-sdks
```

- PowerShell 5.1+。

## 開發快速開始

### 0) PowerShell 前置設定（目前視窗）

這個設定會套用到目前 PowerShell 視窗後續的所有命令：

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
```

### 1) 環境自檢

```powershell
.\scripts\check-developer-environment.ps1
```

如果你要測 Debug 組態，可改用：

```powershell
.\scripts\check-developer-environment.ps1 -Configuration Debug
```

### 2) 還原 / 建置 / 測試

```powershell
dotnet restore .\InvoiceIntelligenceAssistant.slnx -v minimal
dotnet build .\InvoiceIntelligenceAssistant.slnx -c Release --no-restore -v minimal
dotnet test .\tests\InvoiceIntelligence.Core.Tests\InvoiceIntelligence.Core.Tests.csproj -c Release -v minimal
```

### 3) 由原始碼執行

```powershell
dotnet run --project .\src\InvoiceLauncher\InvoiceLauncher.csproj
dotnet run --project .\src\InvoiceDownloader\InvoiceDownloader.csproj
dotnet run --project .\src\InvoiceAiProcessor\InvoiceAiProcessor.csproj
dotnet run --project .\src\InvoiceTrainer\InvoiceTrainer.csproj
```

## 打包與部署

### 發佈腳本

```powershell
.\scripts\publish-downloader.ps1
.\scripts\publish-processor.ps1
.\scripts\publish-trainer.ps1
.\scripts\publish-all.ps1
```

常用參數（白話版）：

- `-Configuration Release|Debug`  
  指定建置組態。預設是 `Release`；要追查部署問題時可改用 `Debug`。
- `-Runtime win-x64|win-x86|win-arm64`  
  指定目標 Windows Runtime Identifier（RID）。預設是 `win-x64`。
- `-SelfContained`  
  產生 self-contained 套件（客戶端不用先裝 .NET Runtime）。未指定時就是 framework-dependent。
- `-FrameworkDependent`  
  保留的相容參數，代表 framework-dependent 模式。不可和 `-SelfContained` 同時使用。
- `-SkipDeveloperEnvironmentCheck`  
  跳過打包前的開發環境檢查（`check-developer-environment.ps1`）。只建議在你已確認環境沒問題時使用。
- `-OutputPath <path>`（單模組腳本：`publish-downloader/processor/trainer.ps1`）  
  指定單模組輸出根路徑。預設會輸出到 `build\deploy\InvoiceIntelligence`。
- `-DeployRoot <path>`（僅 `publish-all.ps1`）  
  指定整合打包根目錄。預設是 `build\deploy`，實際套件路徑會是 `build\deploy\InvoiceIntelligence`。
- `-SkipZip`（僅 `publish-all.ps1`）  
  跳過 zip 壓縮。未指定時會在 `build\deploy\artifacts\` 產生 zip 成品。

### 客戶端環境檢查

```powershell
.\scripts\check-customer-environment.ps1 -Profile all -PublishMode framework-dependent -Runtime win-x64 -RequiredRuntimeVersion 8.0.0
```

回傳碼說明：

- `0`：可部署
- `1`：不可部署

## 設定檔說明

你會最常調整的設定檔是：

- `config/downloader.ini`
- `config/processor.ini`

### `downloader.ini`

| 區段 | 欄位 | 是否必填 | 預設值 | 說明 |
|---|---|---|---|---|
| `module` | `name` | 是 | — | 通常為 `InvoiceDownloader` |
| `paths` | `folderA` | 是 | — | 原始輸出資料夾 |
| `paths` | `folderB` | 是 | — | 保留/相容用途路徑 |
| `input` | `customerListFile` | 否 | `config\company-list.xlsx` | 客戶清單工作簿 |
| `output` | `fileNamingRule` | 是 | — | 至少需包含一個 placeholder |
| `output` | `invoiceFileSuffix` | 否 | `invoice` | 僅允許英數、`-`、`_` |
| `output` | `allowanceFileSuffix` | 否 | `allowance` | 僅允許英數、`-`、`_` |
| `mof` | `account` | 是 | — | MOF 帳號 |
| `mof` | `officialApiBaseUrl` | 否* | 官方 MOF URL | 當明確設定 `invoiceDownloadMode=official` 時必填 |
| `mof` | `appId` | 條件式 | 回退到 `appKey` | 若空值則使用 `appKey` |
| `mof` | `appKey` | 是 | — | 同時可作為 appId 回退值 |
| `mof` | `passwordEncrypted` | 否 | 空字串 | Windows DPAPI 加密密碼 |
| `mof` | `invoiceDownloadMode` | 否 | `official` | 目前僅支援 `official` |

### `processor.ini`

| 區段 | 欄位 | 是否必填 | 預設值 | 說明 |
|---|---|---|---|---|
| `module` | `name` | 是 | — | 通常為 `InvoiceAiProcessor` |
| `paths` | `folderA` | 是 | — | 輸入資料夾（僅讀 `.xlsx`） |
| `paths` | `folderB` | 是 | — | AI 輸出資料夾 |
| `paths` | `folderC` | 否 | `data\folder-c-review` | 人工覆核資料夾 |
| `output` | `fileNamingRule` | 是 | — | 使用命名 placeholder |
| `output` | `invoiceOutputLabel` | 否 | `SalesInvoiceDetail-T3` | 輸出檔名尾碼 |
| `output` | `allowanceOutputLabel` | 否 | `PurchaseVoucherDetail-T4` | 輸出檔名尾碼 |
| `output` | `invoiceFileSuffix` | 否 | `invoice` | 來源檔分類尾碼 |
| `output` | `allowanceFileSuffix` | 否 | `allowance` | 來源檔分類尾碼 |
| `review` | `confidenceThreshold` | 否 | `0.80` | 0 到 1 之間小數 |
| `knowledge` | `repositoryFile` | 否 | `data\knowledge\knowledge-cases.json` | 本地知識庫檔案 |

### 命名規則 Placeholder

可用的 placeholder 有：

- `{taxId}`
- `{companyName}`
- `{dateFrom}`
- `{dateTo}`
- `{timestamp}`

命名規則至少要包含一個 placeholder。

## 核心工作流程

### 下載流程

1. 匯入客戶清單（`.xlsx`）。
2. 驗證 MOF 工作階段。
3. 執行批次下載。
4. 在 Folder A 產生每客戶輸出檔與 `download-log-*.log`。

### 處理流程

1. 讀取 Folder A `.xlsx`。
2. 每筆資料依序經過 rule engine -> case cache -> vector RAG -> local heuristic LLM。
3. 依來源檔群組與信心門檻進行分流。
4. 高信心輸出到 Folder B，低信心群組輸出到 Folder C。

### 訓練流程

1. **手動知識回饋分頁**：勾選核准資料並寫回知識庫。
2. **訓練流程分頁**：
   - 選擇資料集 `.xlsx`
   - 驗證資料集
   - 訓練模型（`build/training/staged-model.json`）
   - 評估模型（`build/training/evaluation-report.json`）
   - 發布模型（`build/training/published-model.json` + `publish-metadata.json`）

資料集驗證時，至少要有以下欄位（或其別名）：

- 公司名稱
- 發票日期
- 品名
- 可抵稅標籤
- 建議科目
- 判定原因

治理品質報告會輸出在：

- `build/training/quality-gates/validation-summary.json`
- `build/training/quality-gates/reject-report.json`
- `build/training/quality-gates/conflict-report.json`

## 公開 API 與擴充點

目前這個儲存庫**不提供** HTTP/REST API。

如果你要擴充功能，主要會從 `src/InvoiceIntelligence.Core` 這些介面下手：

- `ICustomerListImporter`
- `IMofAuthenticator`
- `IMofOfficialInvoiceApiClient`
- `IBatchInvoiceDownloader`
- `IOfficialInvoiceExporter`
- `IRuleEngine`
- `ICaseCache`
- `IRagRetriever`
- `ILocalLlmAnalyzer`
- `IKnowledgeCaseRepository`
- `IModuleConfigurationLoader`

## 環境變數

應用模組執行時不依賴自訂環境變數。

PowerShell 腳本目前只會讀取標準系統環境變數：

- `OS`
- `PROCESSOR_ARCHITECTURE`
- `COMPUTERNAME`

## CI 與容器化狀態

就目前儲存庫內容來看：

- 未找到 Dockerfile。
- `.github/workflows` 下未找到 GitHub Actions 工作流程。

## 相關文件

- PRD 索引：`docs/prd/00-index.md`
- 主 PRD：`docs/prd/01-master-prd.md`
- 專案定義：`docs/project.md`
- 測試文件：`docs/testing.md`
- 治理政策設定：`docs/config/governance/`
- release gate 設定：`docs/config/release-gate/`

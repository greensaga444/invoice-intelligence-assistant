# 發票智慧助理

以在地部署為優先的 Windows 桌面解決方案，用於發票下載與 AI 輔助會計分析。

## 產品概述

發票智慧助理協助會計團隊在本地基礎設施內，自動化處理大量發票作業流程。

核心成果：

- 匯入客戶清單與日期區間。
- 以批次模式下載發票。
- 套用分層式 AI 決策流程。
- 匯出強化後的 Excel 結果供後續會計作業使用。
- 將核准的人工回饋寫入知識庫。

## 架構

系統由兩個可獨立執行的模組組成。

### 模組 A：發票下載器

- 匯入客戶清單 Excel。
- 讀取統編、公司名稱與日期區間。
- 與 MOF 服務進行驗證。
- 批次下載發票檔案。
- 將原始檔案匯出到 Folder A。

### 模組 B：發票 AI 處理器

- 從 Folder A 讀取原始檔案。
- 優先執行規則引擎。
- 透過案例快取重用歷史判定。
- 執行 RAG 檢索取得脈絡案例。
- 對未解決紀錄使用本地 LLM 推理。
- 將強化檔案匯出到 Folder B。

決策流程：

客戶清單 -> 發票下載器 -> Folder A -> 規則引擎 -> 案例快取 -> RAG -> LLM -> Folder B -> 人工知識回饋

## 技術堆疊

桌面端：

- C#
- .NET 8
- WPF

AI 與檢索：

- Ollama
- Qwen3 8B
- BGE-M3
- Qdrant

設定與資料：

- INI 設定檔
- 以 Excel 為主的輸入與輸出
- 本地檔案系統資料夾

## 產品範圍

包含：

- 客戶清單匯入
- 發票下載
- 本地 AI 分析
- 強化版 Excel 匯出
- 命名規則設定
- 人工知識回饋

不包含：

- ERP 整合
- 行動應用程式
- 公有雲部署
- 未經核准的自動學習
- 多租戶 SaaS

## 非功能性目標

- 支援 Windows 10 與 Windows 11
- 啟動時間低於 5 秒
- 每批支援 150 家客戶公司
- 每次執行可處理 15,000 至 30,000 筆發票
- 檔案產生成功率高於 99%
- 不呼叫外部 AI 推理服務

## 在地化政策

- 開發與技術文件：英文
- 使用者介面、輸出內容與訊息：繁體中文

## 儲存庫文件

- 產品需求索引：docs/prd/00-index.md
- 主 PRD：docs/prd/01-master-prd.md
- 專案定義與路線圖：docs/project.md
- 分支執行流程：docs/status.md
- 程式風格基線與語言附錄：docs/style/

## 目前可執行骨架

- 解決方案檔：InvoiceIntelligenceAssistant.slnx
- 應用程式：src/InvoiceDownloader、src/InvoiceAiProcessor
- 共用核心：src/InvoiceIntelligence.Core
- 設定檔範本：config/downloader.ini、config/processor.ini
- Downloader 已支援客戶清單 Excel 匯入、MOF 驗證，以及批次下載原始發票 Excel 至 Folder A 並輸出下載記錄。
- AI Processor 已支援 Folder A 原始發票讀取、分階段 AI 決策流程（Rule Engine -> Case Cache -> RAG -> LLM），並可將保留原始欄位且附加 AI 欄位的強化 Excel 匯出至 Folder B。
- AI Processor 已支援手動知識回饋：可勾選人工核准紀錄並更新本地知識庫，且後續執行會將已核准案例載入 RAG 檢索。
- 目前版本已完成在地化與品質閘道：使用者可見訊息與輸出欄位維持繁體中文，且已加入離線 AI 驗證與目標規模效能測試。

### 本機驗證指令

```powershell
dotnet build InvoiceIntelligenceAssistant.slnx -v minimal
dotnet test InvoiceIntelligenceAssistant.slnx -v minimal
dotnet format InvoiceIntelligenceAssistant.slnx --verify-no-changes
```

兩個桌面模組會在啟動時讀取對應 INI 設定並驗證命名規則占位符。

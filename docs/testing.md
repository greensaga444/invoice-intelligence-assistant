# Testing Catalog

This document lists all current automated tests in the repository, grouped by testing categories.

## Testing Categories Overview

- Category A: Configuration Tests
- Category B: Downloader Tests
- Category C: Processor Tests
- Category D: Integration and Quality-Gate Tests

## Category A: Configuration Tests

### A1. IniFileContentBuilderTests (3)

- BuildDownloaderContent_WhenAppIdIsMissing_UsesAppKeyForBothFields
- BuildDownloaderContent_WhenBothAppIdAndAppKeyProvided_PreservesBothValues
- BuildProcessorContent_WhenAppKeyIsMissing_UsesAppIdForBothFields

### A2. IniModuleConfigurationLoaderTests (8)

- Load_ValidIni_ReturnsConfiguration
- Load_KnowledgeRepositoryPathProvided_UsesConfiguredPath
- Load_UnknownPlaceholder_ThrowsFormatException
- Load_MissingRequiredKey_ThrowsInvalidDataException
- Load_OfficialMode_ParsesBaseUrl
- Load_OfficialModeWithoutBaseUrl_ThrowsInvalidDataException
- Load_AppKeyOnly_UsesAppKeyAsCredentialKey
- Load_WithoutAppKey_ThrowsInvalidDataException

### A3. NamingRuleValidatorTests (5)

- Validate_NullOrWhitespace_ThrowsFormatException
- Validate_WithoutPlaceholders_ThrowsFormatException
- Validate_WithKnownPlaceholdersInDifferentCase_DoesNotThrow
- Validate_WithUnknownPlaceholder_ThrowsFormatException
- Validate_WithDuplicateUnknownPlaceholderInDifferentCase_ReportsOnlyOnce

### A4. OutputFileNameHelperTests (6)

- BuildFileName_ReplacesPlaceholdersAndAddsMissingExtension
- BuildFileName_DoesNotDuplicateExtensionWhenAlreadyPresent
- BuildFileName_ReplacesInvalidCharactersInSegments
- BuildFileName_WithWhitespaceExtension_ThrowsArgumentException
- EnsureUniquePath_WhenFileDoesNotExist_ReturnsSamePath
- EnsureUniquePath_WhenBaseAndSecondCandidateExist_ReturnsThirdCandidate

## Category B: Downloader Tests

### B1. CustomerListExcelImporterTests (3)

- Import_ValidWorkbook_ReturnsRecords
- Import_MissingRequiredColumn_ThrowsInvalidDataException
- Import_InvalidDateRange_ThrowsInvalidDataException

### B2. LocalBatchInvoiceDownloaderTests (4)

- DownloadAsync_ValidRequest_CreatesRawInvoiceFilesAndLog
- DownloadAsync_EmptySessionToken_ThrowsInvalidOperationException
- DownloadAsync_DuplicateGeneratedFileName_UsesUniqueFileNames
- DownloadAsync_OneHundredFiftyCustomers_MeetsBatchScaleAndSuccessRateTarget

### B3. LocalMofAuthenticatorTests (3)

- Authenticate_ValidCredential_ReturnsSuccess
- Authenticate_PlaceholderAccount_ReturnsFailure
- Authenticate_EmptyPassword_ReturnsFailure

### B4. MofOfficialHttpInvoiceApiClientTests (4)

- QueryInvoicesAsync_ValidOfficialResponse_UsesOfficialEndpointAndMapsRecords
- QueryInvoicesAsync_BusinessCodeNot200_ThrowsMofOfficialInvoiceApiException
- QueryInvoicesAsync_MissingOfficialCredential_ThrowsInvalidOperationException
- QueryInvoicesAsync_AllowanceFieldsPresent_MapsAllowanceAmountAndType

### B5. OfficialInvoiceBatchDownloaderTests (5)

- DownloadAsync_MultiDirectionAndPaging_MergesResultsAndDeduplicates
- DownloadAsync_AllFourScopesEnabled_QueriesEachDirectionAndDocumentType
- DownloadAsync_TransientError_RetriesAndSucceeds
- DownloadAsync_TransientErrorExceedsMaxRetry_Throws
- DownloadAsync_NoDirectionSelected_ThrowsInvalidOperationException

### B6. OfficialInvoiceFolderAExcelExporterTests (2)

- ExportAsync_ValidInvoices_WritesOfficialTwoSheetWorkbook
- ExportAsync_EmptyInvoices_WritesHeadersOnly

## Category C: Processor Tests

### C1. FolderAInvoiceRecordReaderTests (3)

- ReadFromFolder_ValidWorkbook_ReadsInvoiceRecords
- ReadFromFolder_MissingRequiredHeader_ThrowsInvalidDataException
- ReadFromFolder_OfficialWorkbook_ReadsInvoiceRecords

### C2. FolderBEnhancedInvoiceExporterTests (3)

- Export_ValidDecisionResults_WritesEnhancedWorkbookWithPreservedAndAiColumns
- Export_WithTimestampPlaceholder_AppliesConfiguredNamingRule
- Export_TwoSourceFilesWithSameNamingResult_UsesUniqueFileNames

### C3. InvoiceDecisionPipelineTests (3)

- Process_RuleEngineResolvesRecord_SkipsCaseCacheRagAndLlm
- Process_CaseCacheResolvesAfterRuleMiss_SkipsRagAndLlm
- Process_UnresolvedRecord_RunsRagThenLlmAndReturnsStructuredDecision

### C4. JsonKnowledgeCaseRepositoryTests (3)

- SaveApprovedResults_WithApprovedRows_PersistsAndCanBeRetrievedByRag
- SaveApprovedResults_DuplicateRows_AreSkipped
- ApplyApprovedResults_NoSelection_DoesNotWriteKnowledgeRepository

## Category D: Integration and Quality-Gate Tests

### D1. Downloader Integration Tests

- OfficialBatchInvoiceDownloaderIntegrationTests
  - DownloadAsync_ValidRequest_ExportsWorkbookAndCreatesLog

### D2. Localization and Quality-Gate Tests

- LocalizationAndQualityGateTests
  - LocalHeuristicLlmAnalyzer_ReturnsTraditionalChineseBusinessFields
  - FolderBEnhancedInvoiceExporter_UsesTraditionalChineseWorksheetName
  - ProcessorCodePath_DoesNotContainExternalAiInvocationPatterns
  - InvoiceDecisionPipeline_ProcessThirtyThousandRecords_CompletesWithinScaleTarget

## Test Inventory Summary

- Test classes: 16
- Test cases: 60
- Primary automated test project: tests/InvoiceIntelligence.Core.Tests

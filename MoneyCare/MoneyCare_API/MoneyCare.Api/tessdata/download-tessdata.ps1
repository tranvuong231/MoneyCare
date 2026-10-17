# Tải dữ liệu ngôn ngữ cho Tesseract OCR (Windows PowerShell). Chạy 1 lần trong thư mục này.
Set-Location $PSScriptRoot
Invoke-WebRequest https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -OutFile eng.traineddata
Invoke-WebRequest https://github.com/tesseract-ocr/tessdata/raw/main/vie.traineddata -OutFile vie.traineddata
Write-Host "Xong: eng.traineddata, vie.traineddata"

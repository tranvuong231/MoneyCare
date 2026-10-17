#!/usr/bin/env bash
# Tải dữ liệu ngôn ngữ cho Tesseract OCR (chạy 1 lần, trong thư mục này).
set -e
cd "$(dirname "$0")"
curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
curl -L -o vie.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/vie.traineddata
echo "Xong: eng.traineddata, vie.traineddata"

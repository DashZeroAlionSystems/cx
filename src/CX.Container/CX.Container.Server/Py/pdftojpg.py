import sys
import os
from pdf2image import convert_from_path


def pdf_to_jpg(pdf_path, output_folder, poppler_path):
    # Ensure the output folder exists
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    # Convert the PDF to images
    pages = convert_from_path(pdf_path, 300, poppler_path=poppler_path)  # 300 DPI

    # Save each page as a JPEG
    for i, page in enumerate(pages):
        image_path = os.path.join(output_folder, f'page {i+1}.jpg')
        page.save(image_path, 'JPEG')
        print(f'Saved {image_path}')


# Usage
pdf_path = sys.argv[1]
output_folder = sys.argv[2]
pdf_to_jpg(pdf_path, output_folder, sys.argv[3])
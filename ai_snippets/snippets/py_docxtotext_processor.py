import docx
import sys


def extract_text_and_pages(docx_path):
	doc = docx.Document(docx_path)
	text_by_page = []
	current_page_text = []
	current_page_number = 1

	for paragraph in doc.paragraphs:
		current_page_text.append(paragraph.text)
		for run in paragraph.runs:
			if run.text == '\f':
				text_by_page.append((current_page_number, '\n'.join(current_page_text)))
				current_page_text = []
				current_page_number += 1

	if current_page_text:
		text_by_page.append((current_page_number, '\n'.join(current_page_text)))

	return text_by_page


def main():
	input_docx = sys.argv[1]
	output_md = sys.argv[2]
	text_pages = extract_text_and_pages(input_docx)
	with open(output_md, "w", encoding="utf-8") as md_file:
		for page_number, text in text_pages:
			md_file.write(f'\n--- PAGE {page_number} ---\n{text}')


if __name__ == "__main__":
	main()
import sys
import json
from io import StringIO
from contextlib import redirect_stderr
from tabulate import tabulate
from operator import itemgetter
import pdfplumber


def check_bboxes(word, table_bbox):
    """
    Check whether the word is inside the table's bounding box.
    """
    word_bbox = (word['x0'], word['top'], word['x1'], word['bottom'])
    table_bbox = (table_bbox[0], table_bbox[1], table_bbox[2], table_bbox[3])

    return (word_bbox[0] < table_bbox[2] and word_bbox[2] > table_bbox[0] and
            word_bbox[1] < table_bbox[3] and word_bbox[3] > table_bbox[1])


def extract_text_and_tables(pdf_path):
    md = []
    
    with pdfplumber.open(pdf_path) as pdf:
        page_no = 0
        print ('[')
        print (json.dumps({ "pages": pdf.pages.__len__() }), end = "")
        
        for page in pdf.pages:
            page_no += 1      
            pageRes = { "page": page_no }      
            
            pageErrorsIO = StringIO();
            
            try:
                with redirect_stderr(pageErrorsIO):
                    md.append(f"--- PAGE {page_no} ---\n\n")
                    tables = page.find_tables() or []
                    table_bboxes = [table.bbox for table in tables] if tables else []
                    tables = [{'table': table.extract(), 'top': table.bbox[1]} for table in tables] if tables else []
                    non_table_words = [word for word in page.extract_words(x_tolerance=1) or [] if not any(
                        check_bboxes(word, table_bbox) for table_bbox in table_bboxes)]

                    for cluster in pdfplumber.utils.cluster_objects(
                            non_table_words + tables, itemgetter('top'), tolerance=5, preserve_order=True):
                        temp_text = []  # Temporary list to collect sequential text

                        for element in cluster:
                            if isinstance(element, dict) and 'text' in element:
                                temp_text.append(element['text'])
                            elif isinstance(element, dict) and 'table' in element:
                                if temp_text:
                                    md.append(' '.join(temp_text) + "\n")
                                    temp_text = []
                                markdown_table = tabulate(element['table'], tablefmt="pipe", headers="firstrow")
                                md.append(markdown_table + "\n\n")

                        if temp_text:
                            md.append(' '.join(temp_text) + "\n")
                
                pageErrors = pageErrorsIO.getvalue()
                pageErrors = str.strip(pageErrors)
            except Exception as ex:
               pageErrors = repr(ex)
            
            if pageErrors and not str.isspace(pageErrors):
                pageRes["errors"] = pageErrors

            print(',\n', end = "")
            print (json.dumps(pageRes), end = "")
            
        print ('\n]')
    return "".join(md)


def save_markdown(content, output_file):
    with open(output_file, "w", encoding="utf-8") as md_file:
        for item in content:
            md_file.write(item)


input_pdf = sys.argv[1]
output_md = sys.argv[2]
markdown_content = extract_text_and_tables(input_pdf)
save_markdown(markdown_content, output_md)

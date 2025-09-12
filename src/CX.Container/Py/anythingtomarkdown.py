import sys
import json
from markitdown import MarkItDown

def convert_to_markdown(input_path, output_path, doc_type='pdf'):
    """
    Convert a document to markdown using MarkItDown
    
    Args:
        input_path (str): Path to the input file
        output_path (str): Directory where output should be saved
        doc_type (str): Type of document
    """
    try:
        md = MarkItDown()
        result = md.convert(input_path)
        
        # Save the result to the output path
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(result.text_content)
            
    except Exception as e:
        # Write error information to output file
        error_info = {
            'error': str(e),
            'input_path': input_path,
            'doc_type': doc_type
        }
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(error_info, f)

def main():
    if len(sys.argv) < 3:
        print("Usage: python markitdown_converter.py <input_path> <output_path> [doc_type]")
        sys.exit(1)

    input_path = sys.argv[1]
    output_path = sys.argv[2]
    doc_type = sys.argv[3] if len(sys.argv) > 3 else 'pdf'

    convert_to_markdown(input_path, output_path, doc_type)

if __name__ == "__main__":
    main()
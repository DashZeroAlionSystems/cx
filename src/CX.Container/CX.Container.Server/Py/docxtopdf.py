import sys
from docx2pdf import convert


def main():
    input_docx = sys.argv[1]
    output_pdf = sys.argv[2]
    convert(input_docx, output_pdf)


if __name__ == "__main__":
    main()

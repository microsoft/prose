import random
import pandas as pd
from utils import stringify_serialzed_df
data = {
    'Date': ['2023-01-01 00:00:00',
             '2023-01-02 00:00:00',
             '2023-01-03 00:00:00',
             '2023-01-04 00:00:00',
             '2023-01-05 00:00:00'],  
    'Ticker': ['AAPL', 'GOOG', 'MSFT', 'AMZN', 'FB'],
    'Price': [150.20, 2700.50, 330.80, 3200.00, 350.75],
    'Shares': [100, 50, 200, 30, 80],
    'Transaction': ['Buy', 'Sell', 'Buy', 'Sell', 'Buy']
}
df_EX1 = pd.DataFrame(data)
data2 = {'Name': ['Alice', 'Bob', 'Charlie'],
         'Age': [25, 30, 22],
         'City': ['New York', 'Los Angeles', 'Chicago'],
         "Sex": ['F', "M", "M"]}

df_EX2 = pd.DataFrame(data2)
df_EX1 = pd.DataFrame(data)


DATA_QUES_INSTRUCTION = """Given the tabular data your job is to provide answer to the question asked over the table.
[Example]

Data: 
[Data_format]

Questions: [Ques]
Answer:
"""

EXAMPLES = """Data: 
[Data_format_example1]

[QA1]

Data:
[Data_format_example2]

[QA2]
"""

###### table formats: #########
TF_EX1_MarkdownFormat = '|    | Date                | Ticker   |   Price |   Shares | Transaction   |\n|---:|:--------------------|:---------|--------:|---------:|:--------------|\n|  0 | 2023-01-01 00:00:00 | AAPL     |  150.2  |      100 | Buy           |\n|  1 | 2023-01-02 00:00:00 | GOOG     | 2700.5  |       50 | Sell          |\n|  2 | 2023-01-03 00:00:00 | MSFT     |  330.8  |      200 | Buy           |\n|  3 | 2023-01-04 00:00:00 | AMZN     | 3200    |       30 | Sell          |\n|  4 | 2023-01-05 00:00:00 | FB       |  350.75 |       80 | Buy           |'
TF_EX1_DataMatrixFormat = """[['', 'Date', 'Ticker', 'Price', 'Shares', 'Transaction'], 
[0, '2023-01-01 00:00:00', 'AAPL', 150.2, 100, 'Buy'], 
[1, '2023-01-02 00:00:00', 'GOOG', 2700.5, 50, 'Sell'], 
[2, '2023-01-03 00:00:00', 'MSFT', 330.8, 200, 'Buy'], 
[3, '2023-01-04 00:00:00', 'AMZN', 3200.0, 30, 'Sell'], 
[4, '2023-01-05 00:00:00', 'FB', 350.75, 80, 'Buy']]
"""
TF_EX1_JsonFormat = '{"0":{"Date":2023-01-01 00:00:00,"Ticker":"AAPL","Price":150.2,"Shares":100,"Transaction":"Buy"},"1":{"Date":2023-01-02 00:00:00,"Ticker":"GOOG","Price":2700.5,"Shares":50,"Transaction":"Sell"},"2":{"Date":2023-01-03 00:00:00,"Ticker":"MSFT","Price":330.8,"Shares":200,"Transaction":"Buy"},"3":{"Date":2023-01-04 00:00:00,"Ticker":"AMZN","Price":3200.0,"Shares":30,"Transaction":"Sell"},"4":{"Date":2023-01-05 00:00:00,"Ticker":"FB","Price":350.75,"Shares":80,"Transaction":"Buy"}}'
TF_EX1_DFloaderFormat = "pd.DataFrame({Date : ['2023-01-01 00:00:00', '2023-01-02 00:00:00', '2023-01-03 00:00:00', '2023-01-04 00:00:00', '2023-01-05 00:00:00'], Ticker : ['AAPL', 'GOOG', 'MSFT', 'AMZN', 'FB'], Price : [150.2, 2700.5, 330.8, 3200.0, 350.75], Shares : [100, 50, 200, 30, 80], Transaction : ['Buy', 'Sell', 'Buy', 'Sell', 'Buy']}, index=[0, 1, 2, 3, 4])"
TF_EX1_HTMLFormat = '<table border="1" class="dataframe">\n  <thead>\n    <tr style="text-align: right;">\n      <th></th>\n      <th>Date</th>\n      <th>Ticker</th>\n      <th>Price</th>\n      <th>Shares</th>\n      <th>Transaction</th>\n    </tr>\n  </thead>\n  <tbody>\n    <tr>\n      <th>0</th>\n      <td>2023-01-01 00:00:00</td>\n      <td>AAPL</td>\n      <td>150.20</td>\n      <td>100</td>\n      <td>Buy</td>\n    </tr>\n    <tr>\n      <th>1</th>\n      <td>2023-01-02 00:00:00</td>\n      <td>GOOG</td>\n      <td>2700.50</td>\n      <td>50</td>\n      <td>Sell</td>\n    </tr>\n    <tr>\n      <th>2</th>\n      <td>2023-01-03 00:00:00</td>\n      <td>MSFT</td>\n      <td>330.80</td>\n      <td>200</td>\n      <td>Buy</td>\n    </tr>\n    <tr>\n      <th>3</th>\n      <td>2023-01-04 00:00:00</td>\n      <td>AMZN</td>\n      <td>3200.00</td>\n      <td>30</td>\n      <td>Sell</td>\n    </tr>\n    <tr>\n      <th>4</th>\n      <td>2023-01-05 00:00:00</td>\n      <td>FB</td>\n      <td>350.75</td>\n      <td>80</td>\n      <td>Buy</td>\n    </tr>\n  </tbody>\n</table>'
TF_EX1_HTMLNoSpaceFormat = '<table border="1" class="dataframe">  <thead><tr style="text-align: right;">  <th></th>  <th>Date</th>  <th>Ticker</th>  <th>Price</th>  <th>Shares</th>  <th>Transaction</th></tr>  </thead>  <tbody><tr>  <th>0</th>  <td>2023-01-01 00:00:00</td>  <td>AAPL</td>  <td>150.20</td>  <td>100</td>  <td>Buy</td></tr><tr>  <th>1</th>  <td>2023-01-02 00:00:00</td>  <td>GOOG</td>  <td>2700.50</td>  <td>50</td>  <td>Sell</td></tr><tr>  <th>2</th>  <td>2023-01-03 00:00:00</td>  <td>MSFT</td>  <td>330.80</td>  <td>200</td>  <td>Buy</td></tr><tr>  <th>3</th>  <td>2023-01-04 00:00:00</td>  <td>AMZN</td>  <td>3200.00</td>  <td>30</td>  <td>Sell</td></tr><tr>  <th>4</th>  <td>2023-01-05 00:00:00</td>  <td>FB</td>  <td>350.75</td>  <td>80</td>  <td>Buy</td></tr>  </tbody></table>'

TF_EX1_TabSeparatedFormat = """	Date	Ticker	Price	Shares	Transaction
0	2023-01-01	AAPL	150.2	100	Buy
1	2023-01-02	GOOG	2700.5	50	Sell
2	2023-01-03	MSFT	330.8	200	Buy
3	2023-01-04	AMZN	3200.0	30	Sell
4	2023-01-05	FB	350.75	80	Buy"""
TF_EX1_CommaSeparatedFormat = """,Date,Ticker,Price,Shares,Transaction
0,2023-01-01,AAPL,150.2,100,Buy
1,2023-01-02,GOOG,2700.5,50,Sell
2,2023-01-03,MSFT,330.8,200,Buy
3,2023-01-04,AMZN,3200.0,30,Sell
4,2023-01-05,FB,350.75,80,Buy"""

######################################
###### Table ops Questionaire: #########


def Example_TransposeTests(FormatName, TransposeTable, SerializeTable, ColumnCluster, df_EX1=df_EX1, df_EX2=df_EX2):
    Recontruction_Example_str = ""
    Recontruction_Example_str += f"Data:\n{FormatName.formatting(df_EX1)}\n\n"
    Recontruction_Example_str += f"""Question: Can you transpose the table?
Answer:
{FormatName.formatting(next(TransposeTable().modify(df_EX1)))}\n\n\n"""
    Recontruction_Example_str += f"Data:\n{FormatName.formatting(df_EX2)}\n\n"
    Recontruction_Example_str += f"""Question: Can you transpose the table?
Answer:
{FormatName.formatting(next(TransposeTable().modify(df_EX2)))}\n\n"""
    return Recontruction_Example_str


def Example_ColumnReorder(FormatName, TransposeTable, SerializeTable, ColumnCluster, df_EX1=df_EX1, df_EX2=df_EX2):
    Reordering_Example_str = ""
    col_suffled1 = df_EX1.columns.to_list()
    random.shuffle(col_suffled1)
    Reordering_Example_str += f"Data:\n{FormatName.formatting(df_EX1)}\n\n"
    Reordering_Example_str += f"""Question: Can you reorder the table such that the column are in this new order {str(col_suffled1)}?
Answer:
{FormatName.formatting(df_EX1[col_suffled1])}\n\n\n"""
    col_suffled2 = df_EX2.columns.to_list()
    random.shuffle(col_suffled2)
    Reordering_Example_str += f"Data:\n{FormatName.formatting(df_EX2)}\n\n"
    Reordering_Example_str += f"""Question: Can you reorder the table such that the column are in this new order {str(col_suffled2)} ?
Answer:
{FormatName.formatting(df_EX2[col_suffled2])}\n"""
    return Reordering_Example_str


def Example_Reconstruction(FormatName, TransposeTable, SerializeTable, ColumnCluster, df_EX1=df_EX1, df_EX2=df_EX2):
    Recontruction_Example_str = ""
    Recontruction_Example_str += f"Data:\n{FormatName.formatting(next(SerializeTable().modify(df_EX1)))}\n\n"
    Recontruction_Example_str += f"""Question: Can you reconstruct the table by deserializing the table above?
Answer:
{FormatName.formatting(df_EX1)}\n\n\n"""
    Recontruction_Example_str += f"Data:\n{FormatName.formatting(next(ColumnCluster().modify(df_EX2)))}\n\n"
    Recontruction_Example_str += f"""Question: Can you reconstruct the table by deserializing the table above?
Answer:
{FormatName.formatting(df_EX2)}\n\n"""
    return Recontruction_Example_str


def Example_Reconstruction1(FormatName, TransposeTable, SerializeTable, ColumnCluster, df_EX1=df_EX1, df_EX2=df_EX2):
    """ Similar to reconstruction but the input table is just string"""
    Recontruction_Example_str = ""
    Recontruction_Example_str += f"Data:\n{stringify_serialzed_df(next(SerializeTable().modify(df_EX1)))}\n\n"
    Recontruction_Example_str += f"""Question: Can you reconstruct the table by deserializing the table above?
Answer:
{FormatName.formatting(df_EX1)}\n\n\n"""
    Recontruction_Example_str += f"Data:\n{stringify_serialzed_df(next(ColumnCluster().modify(df_EX2)))}\n\n"
    Recontruction_Example_str += f"""Question: Can you reconstruct the table by deserializing the table above?
Answer:
{FormatName.formatting(df_EX2)}\n\n"""
    return Recontruction_Example_str
####################################################################


###### Test case Questionaire: #########
Ex1_QA1_NavigationTests = """Question: What value is at row 3 and column Ticker?
Answer:
AMZN"""

Ex1_QA2_NavigationTests = """Question: What value is at row 1 and column Transaction?
Answer:
Sell"""

Ex1_QA1_ColumnLookupTests = """Question: What column is the FB in?
Answer:
Ticker"""

Ex1_QA2_ColumnLookupTests = """Question: What column is the '2023-01-02 00:00:00' in?
Answer:
Date"""

Ex1_QA1_RowLookupTests = """Question: What row is the FB in?
Answer:
4"""

Ex1_QA2_RowLookupTests = """Question: What row is the '2023-01-02 00:00:00' in?
Answer:
1"""

Ex1_QA1_DataTypeLookupTests = """Question: What type (using Pandas datatype notation) is column Shares?
Answer:
int64"""

Ex1_QA2_DataTypeLookupTests = """Question: What type (using Pandas datatype notation) is column Price?
Answer:
float64"""

######################################

###### Example Dictionary defination: #########
EXAMPLE_Dictionary = {"TF_EX1_MarkdownFormat": TF_EX1_MarkdownFormat,
                      "TF_EX1_DataMatrixFormat": TF_EX1_DataMatrixFormat,
                      "TF_EX1_JsonFormat": TF_EX1_JsonFormat,
                      "TF_EX1_DFloaderFormat": TF_EX1_DFloaderFormat,
                      "TF_EX1_HTMLFormat": TF_EX1_HTMLFormat,
                      "TF_EX1_HTMLNoSpaceFormat": TF_EX1_HTMLNoSpaceFormat,
                      "TF_EX1_TabSeparatedFormat": TF_EX1_TabSeparatedFormat,
                      "TF_EX1_CommaSeparatedFormat": TF_EX1_CommaSeparatedFormat,
                      "Ex1_QA1_NavigationTests": Ex1_QA1_NavigationTests,
                      "Ex1_QA2_NavigationTests": Ex1_QA2_NavigationTests,
                      "Ex1_QA1_ColumnLookupTests": Ex1_QA1_ColumnLookupTests,
                      "Ex1_QA2_ColumnLookupTests": Ex1_QA2_ColumnLookupTests,
                      "Ex1_QA1_RowLookupTests": Ex1_QA1_RowLookupTests,
                      "Ex1_QA2_RowLookupTests": Ex1_QA2_RowLookupTests,
                      "Ex1_QA1_DataTypeLookupTests": Ex1_QA1_DataTypeLookupTests,
                      "Ex1_QA2_DataTypeLookupTests": Ex1_QA2_DataTypeLookupTests,
                      "Ex_TableReconstructionTests": Example_Reconstruction,
                      "Ex_TableTransposeTests": Example_TransposeTests,
                      "Ex_TableColumnReorderTests": Example_ColumnReorder,
                      "Ex_TableReconstructionTests1": Example_Reconstruction1


                      }

######################################

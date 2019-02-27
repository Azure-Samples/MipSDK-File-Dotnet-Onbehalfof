/*
 The MIT License (MIT)
 
Copyright (c) 2018 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using OfficeOpenXml;
using MipSdkFileApiDotNet.Controllers;
using MipSdkFileApiDotNet.Models;

namespace MipSdkFileApiDotNet
{
    public partial class _Default : Page
    {
        MipLabelController labelController = new MipLabelController();
        private static List<CustomClass> data = new List<CustomClass>();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                PopulateLabels();
                PopulateDataGrid();
            }
        }

        private void PopulateLabels()
        {
            treeViewLabels.Nodes.Clear();

            var labels = labelController.GetAllLabels();

            foreach (var _label in labels)
            {
                TreeNode node = new TreeNode
                {
                    Text = _label.Name,
                    Value = _label.Id,
                    ToolTip = _label.Description
                };

                if (_label.Children.Count > 0)
                {
                    foreach (var _child in _label.Children)
                    {
                        TreeNode childNode = new TreeNode
                        {
                            Text = _child.Name,
                            Value = _child.Id,
                            ToolTip = _child.Description
                        };
                        node.ChildNodes.Add(childNode);
                    }
                }

                treeViewLabels.Nodes.Add(node);
            }
        }


        // Use WebClient to read data from web service, deserialize, store, and bind to GridView.
        protected void PopulateDataGrid()
        {
            string DataEndpoint = ConfigurationManager.AppSettings["DataEndpoint"];

            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString(DataEndpoint);
                data = JsonConvert.DeserializeObject<List<CustomClass>>(json);
                gridViewData.DataSource = data;
                gridViewData.DataBind();
            }
        }

        protected void TreeViewLabels_SelectedNodeChanged(object sender, EventArgs e)
        {
            labelSelectedLabel.Text = "";

            // Update Label UI element to display the selected MIP Label.
            if (treeViewLabels.SelectedNode.Parent != null)
            {
                labelSelectedLabel.Text = treeViewLabels.SelectedNode.Parent.Text + " \\ ";
            }
            labelSelectedLabel.Text += treeViewLabels.SelectedNode.Text;
        }
       
        protected void ButtonDownload_Click(object sender, EventArgs e)
        {            
            string FileName = "MyAppOutput.xlsx";
            string templateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.xlsx");            

            if (treeViewLabels.SelectedNode == null)
            {
                labelSelectedLabel.Text = "Please select a label";
                return;
            }

            // Using EPPlus, generate a spreadsheet using the data from the web service.
            // Reads a template from app_data. This is a bit of a hack as I had trouble making it work from a new stream.
            MemoryStream excelStream = new MemoryStream();
            using (var Excel = new ExcelPackage(new FileInfo(templateFile)))
            {
                var Worksheet = Excel.Workbook.Worksheets.Add("MyData");
                Excel.Workbook.Worksheets.Delete("Sheet1");
                Worksheet.Cells["A1"].LoadFromCollection(data, true, OfficeOpenXml.Table.TableStyles.Dark10);                
                Excel.SaveAs(excelStream);
            }

                            
            using (var outputStream = new MemoryStream())
            {
                // Pass in Excel stream to ApplyLabel, return result on outputStream and write reponse to client
                bool result = labelController.ApplyLabel(excelStream, FileName, treeViewLabels.SelectedValue, outputStream);
                
                if (result)
                {
                    HttpResponse Response = HttpContext.Current.Response;
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Disposition", "attachment; filename=" + FileName + ";");
                    Response.BinaryWrite(outputStream.ToArray());
                    Response.Flush();
                    Response.End();
                }                
            }
        }
    }
}
<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MipSdkFileApiDotNet._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>MIP SDK .NET Sample!</h1>
        <p class="lead">The Microsoft Information Protection SDK enables Microsoft customers to label and protect information, no matter where it's born.</p>
        <p><a href="https://aka.ms/mipsdk" class="btn btn-primary btn-lg">Learn more &raquo;</a></p>
    </div>

    <div class="wrapper">        
        <div class="box b">
            <asp:TreeView ID="treeViewLabels" runat="server" Width=200px Height=300px SelectedNodeStyle-BackColor="White" SelectedNodeStyle-ForeColor="Black" OnSelectedNodeChanged="TreeViewLabels_SelectedNodeChanged" Font-Bold="True" ForeColor="White" ImageSet="Arrows" >
                <HoverNodeStyle Font-Underline="True" ForeColor="#5555DD" />
                <NodeStyle Font-Names="Tahoma" Font-Size="10pt" ForeColor="Black" HorizontalPadding="5px" NodeSpacing="0px" VerticalPadding="0px" />
                <ParentNodeStyle Font-Bold="False" />
<SelectedNodeStyle ForeColor="#5555DD" Font-Underline="True" HorizontalPadding="0px" VerticalPadding="0px"></SelectedNodeStyle>
            </asp:TreeView>
        </div>
        <div class="box c">
            <asp:Button ID="ButtonDownload" runat="server" Text="Download!"  BackColor="Lime" ForeColor="Black" OnClick="ButtonDownload_Click" Font-Bold="True" Font-Size="Large" Height="52px" Width="160px"  />
        </div>
        <div class="box d">
            <asp:Label ID="labelSelectedLabel" runat="server" Text="null" Font-Bold="True" Font-Size="X-Large"></asp:Label>
        </div>
        <div class="box a">
             <asp:GridView ID="gridViewData" runat="server" Width="1000px" Height="219px" AllowPaging="True" AllowCustomPaging="True" PageSize="20" AlternatingRowStyle-ForeColor="Black" AlternatingRowStyle-BackColor="LightBlue" size CellPadding="4" ForeColor="#333333" GridLines="None">
<AlternatingRowStyle BackColor="White"></AlternatingRowStyle>
                 <EditRowStyle BackColor="#2461BF" />
                 <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                 <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                 <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
                 <RowStyle BackColor="#EFF3FB" />
                 <SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
                 <SortedAscendingCellStyle BackColor="#F5F7FB" />
                 <SortedAscendingHeaderStyle BackColor="#6D95E1" />
                 <SortedDescendingCellStyle BackColor="#E9EBEF" />
                 <SortedDescendingHeaderStyle BackColor="#4870BE" />
             </asp:GridView>
        </div>
    </div>

</asp:Content>

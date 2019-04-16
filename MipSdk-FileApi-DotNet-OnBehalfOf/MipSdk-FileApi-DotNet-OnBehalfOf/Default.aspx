<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MipSdkFileApiDotNet._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>MIP SDK .NET Sample!</h1>
        <p class="lead">The Microsoft Information Protection SDK enables Microsoft customers to label and protect information, no matter where it's born.</p>
        <p><a href="https://aka.ms/mipsdk" class="btn btn-primary btn-lg">Learn more &raquo;</a></p>
    </div>

    <div class="wrapper">        
        <div class="box b">
            <asp:TreeView ID="treeViewLabels" runat="server" ShowLines="True" Width=200 Height=300  ExpandDepth="FullyExpand" SelectedNodeStyle-BackColor="White" SelectedNodeStyle-ForeColor="Black" OnSelectedNodeChanged="TreeViewLabels_SelectedNodeChanged" ></asp:TreeView>
        </div>
        <div class="box c">
            <asp:Button ID="ButtonDownload" runat="server" Text="Download!"  BackColor="Black" ForeColor="Red" OnClick="ButtonDownload_Click"  />
        </div>
        <div class="box d">
            <asp:Label ID="labelSelectedLabel" runat="server" Text="null"></asp:Label>
        </div>
        <div class="box a">
             <asp:GridView ID="gridViewData" runat="server" Width="1000" Height="219px" AllowPaging="True" AllowCustomPaging="True"  AutoGenerateColumns="true" PageSize="20">
             </asp:GridView>
        </div>
    </div>

</asp:Content>

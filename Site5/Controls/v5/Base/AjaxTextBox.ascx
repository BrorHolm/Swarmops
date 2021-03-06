﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AjaxTextBox.ascx.cs" Inherits="Swarmops.Frontend.Controls.v5.Base.AjaxTextBox" %>

<script type="text/javascript">
    $(document).ready(function () {

        var _initVal_<%=this.TextInput.ClientID%> = $('#<%=this.TextInput.ClientID%>').val();

        $('#<%=this.TextInput.ClientID%>').blur(function () {

            var currentValue = $('#<%=this.TextInput.ClientID%>').val();

            if (currentValue != _initVal_<%=this.TextInput.ClientID%>) {
                var jsonData = {};
                jsonData.newValue = currentValue;
                jsonData.cookie = '<%=this.Cookie%>';

                // The AJAX call expects the following function prototype:
                //
                // [WebMethod]
                // public static AjaxTextBox.CallbackResult FunctionName (string newValue, string cookie);
                //
                // The page must include IncludedControls.JsonParameters and IncludedControls.ColorAnimation among its script.
                //
                // The AJAX return value expects this structure:
                //
                // msg.d {
                //   int ResultCode; // 0 - Unknown, 1 - Success, 2 - [Value was] Changed, 3 - Invalid [value], 4 - NoPermission
                //   string NewData; // string as it should be displayed after change - overrides input and current value
                // };

                $(this).css('background-color', '#FFFFE0');
                $.ajax({
                    type: "POST",
                    url: "<%=this.AjaxCallbackUrl%>",
                    data: $.toJSON(jsonData),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: $.proxy(function (msg) {
                        if (msg.d.ResultCode == 3 || msg.d.ResultCode == 4) { // Invalid or NoPermission
                            if (msg.d.DisplayMessage != null)
                            {
                                alertify.error (msg.d.DisplayMessage);
                            }
                            else
                            {
                                alertify.error("There was an error attempting to set this value."); // TODO: Localize
                            }
                            $(this).css('background-color', '#FFA0A0');
                        } else {  // msg.d.Result should be Changed or Success here
                            if (msg.d.DisplayMessage != null)
                            {
                                alertify.log (msg.d.DisplayMessage);
                            }
                            $(this).css('background-color', '#E0FFE0');
                            _initVal_<%=this.TextInput.ClientID%> = msg.d.NewData;
                        }
                        $(this).val(_initVal_<%=this.TextInput.ClientID%>);
                        $(this).animate({ backgroundColor: "#FFFFFF" }, 250);
                        <%

                            if (!string.IsNullOrEmpty (this.OnChange))
                            {
                                Response.Write (this.OnChange + "(msg.d.NewData);");  // JavaScript callback on successful change
                            }
    
                        %>
                    }, this),
                    error: $.proxy(function (msg) {
                        alertify.error("<%= Resources.Global.Error_AjaxCallException %>");
                        $(this).val (_initVal_<%=this.TextInput.ClientID%>);
                        $(this).css('background-color', '#FFA0A0');
                        $(this).animate({ backgroundColor: "#FFFFFF" }, 250);
                    }, this)
                });
            }

        });

    });

</script>

<asp:TextBox ID="TextInput" runat="server" />

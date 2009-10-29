'
' DotNetNuke� - http://www.dotnetnuke.com
' Copyright (c) 2002-2009
' by DotNetNuke Corporation
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'
Option Strict On
Option Explicit On 

Namespace DotNetNuke.Modules.Forum.UCP

	''' <summary>
	''' This is the users "My Settings" page and also the page used for administrators of
	''' the module to change forum user settings. This is the forum users profile.
	''' </summary>
	''' <remarks>
	''' </remarks>
	''' <history>
	''' 	[skeel]	29/11/2008	Created
	''' </history>
	Partial Public Class Settings
		Inherits ForumModuleBase
		Implements Utilities.AjaxLoader.IPageLoad

#Region "Interfaces"

		''' <summary>
		''' This is required to replace If Page.IsPostBack = False because controls are dynamically loaded via Ajax. 
		''' </summary>
		''' <remarks></remarks>
		Protected Sub LoadInitialView() Implements Utilities.AjaxLoader.IPageLoad.LoadInitialView
			Dim ProfileUser As ForumUser = ForumUserController.GetForumUser(ProfileUserID, False, ModuleId, PortalId)
			Dim objSecurity As New Forum.ModuleSecurity(ModuleId, TabId, -1, UserId)

			' Get from lists table of core. 0 = Text, 1 = HTML
			Dim ctlLists As New DotNetNuke.Common.Lists.ListController
			Dim listEmailFormat As DotNetNuke.Common.Lists.ListEntryInfoCollection = ctlLists.GetListEntryInfoCollection("EmailFormat")

			ddlEmailFormat.Items.Clear()
			For Each entry As DotNetNuke.Common.Lists.ListEntryInfo In listEmailFormat
				Dim formatEntry As New ListItem(Localization.GetString(entry.Text, objConfig.SharedResourceFile), entry.Value)
				ddlEmailFormat.Items.Add(formatEntry)
			Next

			With ProfileUser
				' skin
				' group
				txtPostsPerPage.Text = .PostsPerPage.ToString
				txtThreadsPerPage.Text = .ThreadsPerPage.ToString
				chkOnlineStatus.Checked = .EnableOnlineStatus
				chkEnableMemberList.Checked = .EnableDisplayInMemberList
				chkEnablePM.Checked = .EnablePM
				chkEnableDefaultPostNotify.Checked = .EnableDefaultPostNotify
				chkEnableSelfNotifications.Checked = .EnableSelfNotifications
				chkEnablePMNotifications.Checked = .EnablePMNotifications
				chkEnableForumModNotify.Checked = .EnableModNotification
				ddlEmailFormat.SelectedValue = .EmailFormat.ToString

				If objConfig.EnableUsersOnline Then
					rowOnlineStatus.Visible = True
				Else
					rowOnlineStatus.Visible = False
				End If
			End With

			EnableControls(objSecurity, ProfileUser)
		End Sub

#End Region

#Region "Event Handlers"

		''' <summary>
		''' Updates the users Forum settings
		''' </summary>
		''' <param name="sender">System.Object</param>
		''' <param name="e">System.EventArgs</param>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	7/13/2005	Created
		''' </history>
		Protected Sub cmdUpdate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdUpdate.Click
			Try
				Dim ProfileUser As ForumUser = ForumUserController.GetForumUser(ProfileUserID, False, ModuleId, PortalId)

				With ProfileUser
					.EnableOnlineStatus = chkOnlineStatus.Checked
					.PostsPerPage = Int32.Parse(txtPostsPerPage.Text)
					.ThreadsPerPage = Int32.Parse(txtThreadsPerPage.Text)
					.EnableDisplayInMemberList = chkEnableMemberList.Checked
					.EnableModNotification = chkEnableForumModNotify.Checked
					.EnablePM = chkEnablePM.Checked
					.EnablePMNotifications = chkEnablePMNotifications.Checked
					.EmailFormat = CType(ddlEmailFormat.SelectedValue, Integer)
					.PortalID = PortalId
					.UserID = ProfileUser.UserID
					.EnableDefaultPostNotify = chkEnableDefaultPostNotify.Checked
					.EnableSelfNotifications = chkEnableSelfNotifications.Checked

					Dim cntUser As New ForumUserController
					cntUser.Update(ProfileUser)
					ForumUserController.ResetForumUser(ProfileUser.UserID, PortalId)
				End With

				lblUpdateDone.Visible = True
			Catch Exc As System.Exception
				LogException(Exc)
				Return
			End Try
		End Sub

		''' <summary>
		''' This clears all forum and thread read status items for a single user
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	12/16/2005	Created
		''' </history>
		Protected Sub cmdClearReads_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdClearReads.Click
			DataProvider.Instance().UserDeleteReads(ProfileUserID)
		End Sub

		''' <summary>
		''' Shows/hides the PM Notification area dependant on if the user has
		''' the ability to receive/send PM's or not.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	1/15/2006	Created
		''' </history>
		Protected Sub chkEnablePM_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkEnablePM.CheckedChanged
			If chkEnablePM.Checked Then
				rowPMNotification.Visible = True
			Else
				rowPMNotification.Visible = False
			End If
		End Sub

#End Region

#Region "Private Methods"

		''' <summary>
		''' Shows/Hides controls depending on module settings and user configuration.
		''' </summary>
		''' <remarks></remarks>
		Private Sub EnableControls(ByVal objSecurity As Forum.ModuleSecurity, ByVal ProfileUser As ForumUser)
			If objConfig.EnableMemberList Then
				rowMemberList.Visible = True
			Else
				rowMemberList.Visible = False
			End If

			If objConfig.EnablePMSystem Then
				rowEnablePM.Visible = True
				If ProfileUser.EnablePM Then
					If objConfig.MailNotification Then
						rowPMNotification.Visible = True
					Else
						rowPMNotification.Visible = False
					End If
				Else
					rowPMNotification.Visible = False
				End If
			Else
				rowEnablePM.Visible = False
				rowPMNotification.Visible = False
			End If
		End Sub

#End Region

	End Class

End Namespace
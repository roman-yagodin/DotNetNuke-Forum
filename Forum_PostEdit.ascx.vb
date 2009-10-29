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

Namespace DotNetNuke.Modules.Forum

	''' <summary>
	''' This is where all posts are added and/or edited from.  It also fires off 
	''' email notification and factors in moderation.
	''' </summary>
	''' <remarks>
	''' </remarks>
	Public MustInherit Class PostEdit
		Inherits ForumModuleBase
		Implements DotNetNuke.Entities.Modules.IActionable

#Region "Private Members"

		Const COLUMN_DELETE As Integer = 0
		Const COLUMN_MOVE_DOWN As Integer = 1
		Const COLUMN_MOVE_UP As Integer = 2
		Const COLUMN_ANSWER As Integer = 3

#End Region

#Region "Optional Interfaces"

		''' <summary>
		''' Gets a list of module actions available to the user to provide it to DNN core.
		''' </summary>
		''' <value></value>
		''' <returns>The collection of module actions available to the user</returns>
		''' <remarks></remarks>
		Public ReadOnly Property ModuleActions() As DotNetNuke.Entities.Modules.Actions.ModuleActionCollection Implements Entities.Modules.IActionable.ModuleActions
			Get
				Return Utilities.ForumUtils.PerUserModuleActions(objConfig, Me)
			End Get
		End Property

#End Region

#Region "Event Handlers"

		''' <summary>
		''' Used to setup the client side reorder datagrid.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks></remarks>
		''' 'This call is required by the Web Form Designer.
		Protected Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
			For Each column As DataGridColumn In dgAnswers.Columns
				If column.GetType() Is GetType(DotNetNuke.UI.WebControls.ImageCommandColumn) Then
					Dim imageColumn As DotNetNuke.UI.WebControls.ImageCommandColumn = CType(column, DotNetNuke.UI.WebControls.ImageCommandColumn)
					Select Case imageColumn.CommandName
						Case "Delete"
							imageColumn.OnClickJS = Localization.GetString("DeleteItem")
							imageColumn.Text = Localization.GetString("Delete", Me.LocalResourceFile)
							imageColumn.ImageURL = objConfig.GetThemeImageURL("s_delete.") & objConfig.ImageExtension
						Case "MoveUp"
							imageColumn.Text = Localization.GetString("MoveUp", Me.LocalResourceFile)
							imageColumn.ImageURL = objConfig.GetThemeImageURL("s_up.") & objConfig.ImageExtension
						Case "MoveDown"
							imageColumn.Text = Localization.GetString("MoveDown", Me.LocalResourceFile)
							imageColumn.ImageURL = objConfig.GetThemeImageURL("s_down.") & objConfig.ImageExtension
					End Select
				End If
			Next
		End Sub

		''' <summary>
		''' The Page Load request of this control.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		Protected Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
			Try
				Dim securityForumID As Integer = -1
				Dim objParentPost As New PostInfo
				objParentPost = Nothing

				' If this is not a new thread, obtain original post/thread
				If Not Request.QueryString("postid") Is Nothing Then
					Dim PostID As Integer = -1

					PostID = Int32.Parse(Request.QueryString("postid"))
					objParentPost = PostInfo.GetPostInfo(PostID, PortalId)
					securityForumID = objParentPost.ForumID
				End If

				Dim ForumID As Integer = -1
				Dim objForum As New ForumInfo

				' Obtain forum info object
				If Not Request.QueryString("forumid") Is Nothing Then
					ForumID = Int32.Parse(Request.QueryString("forumid"))
					If ForumID <> -1 Then
						objForum = ForumInfo.GetForumInfo(ForumID)
						securityForumID = objForum.ForumID
						' if the forumid in the querystring does not match the one assigned to the postid, someone is editing the querystring trying to gain posting access they may not have
						If Not objParentPost Is Nothing Then
							If Not ForumID = objParentPost.ParentThread.ForumID Then
								HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
							End If
						End If
					End If
				Else
					' This is done here to make sure we have the current ForumID set properly. (Noticed previous issues in security class when not doing this)
					securityForumID = objForum.ForumID
				End If

				With Me.cmdCalEndDate
					.ImageUrl = objConfig.GetThemeImageURL("s_calendar.") & objConfig.ImageExtension
					.NavigateUrl = CType(Common.Utilities.Calendar.InvokePopupCal(txtEndDate), String)
					.ToolTip = Localization.GetString("cmdCalEndDate", LocalResourceFile)
				End With

				If Page.IsPostBack = False Then
					litCSSLoad.Text = "<link href='" & objConfig.Css & "' type='text/css' rel='stylesheet' />"
					Localization.LocalizeDataGrid(dgAnswers, Me.LocalResourceFile)

					txtPollID.Text = "-1"
					Dim Security As New Forum.ModuleSecurity(ModuleId, TabId, securityForumID, UserId)
					Dim objForumUser As ForumUser = Nothing
					Dim objLoggedOnUserID As Integer = -1

					' Before we do anything, see if the user is logged in and has permission to be here
					objLoggedOnUserID = Users.UserController.GetCurrentUserInfo.UserID
					If Request.IsAuthenticated And objLoggedOnUserID > 0 Then
						objForumUser = ForumUserController.GetForumUser(objLoggedOnUserID, False, ModuleId, PortalId)

						' Before anything else, make sure the user is not banned
						If objForumUser.IsBanned Then
							HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
						End If
					Else
						HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
					End If

					' Make sure user is not accessing a private forum via qs to reply/start new thread (and if they are, make sure they have perms)
					If Not objForum.PublicView Then
						If (Security.IsAllowedToViewPrivateForum = False) Then
							HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
						End If
					End If

					' Obtain post action
					Dim objAction As PostAction

					If (Not Request.QueryString("action") Is Nothing) Then
						objAction = CType([Enum].Parse(GetType(PostAction), Request.QueryString("action"), True), PostAction)
						cmdSubmit.CommandName = objAction.ToString
					End If

					If objAction <> PostAction.[New] AndAlso Not objParentPost Is Nothing Then
						If objParentPost.ParentPostID <> 0 Then chkIsPinned.Enabled = False
						If objParentPost.ParentPostID <> 0 Then chkIsClosed.Enabled = False
					End If

					Dim AllowUserEdit As Boolean = False

					' Check to see what type of action we are doing here, our only concern is edit
					Select Case objAction
						Case PostAction.Edit
							' security check
							If Not objParentPost.ParentThread.HostForum.PublicPosting Then
								'restricted posting forum
								If Not (Security.IsAllowedToPostRestrictedReply Or Security.IsAllowedToStartRestrictedThread) Then
									HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
								End If
							End If

							' Make sure user IsTrusted too before they can edit (but only if a moderated forum, if its not moderated we don't care)
							' First check to see if user is original author 
							If objLoggedOnUserID > 0 And (objParentPost.UserID = objForumUser.UserID) And (objParentPost.ParentThread.HostForum.IsModerated = False Or objForumUser.IsTrusted Or Security.IsUnmoderated) And (objParentPost.ParentThread.HostForum.IsActive) Then
								AllowUserEdit = True
							Else
								' The user is not the original author
								' See if they are admin or moderator - always have to be some type of mod or admin to edit (aka logged in)
								If objLoggedOnUserID > 0 And (Security.IsForumModerator = True) Then
									AllowUserEdit = True
								End If
							End If
						Case PostAction.[New]
							' security check
							If Not objForum.PublicPosting Then
								'restricted posting forum
								If Not (Security.IsAllowedToStartRestrictedThread) Then
									HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
								End If
							End If

							If objLoggedOnUserID > 0 And (objForum.IsActive = True) Then
								' have to add some check to make sure the forum is still active

								' Rework if allowing anonymous posting, for now this is good
								AllowUserEdit = True
							End If
						Case Else
							' If we reach this point, we know it is a reply, quote

							' security check
							If Not objParentPost.ParentThread.HostForum.PublicPosting Then
								'restricted posting forum
								If Not (Security.IsAllowedToPostRestrictedReply) Then
									HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
								End If
							End If

							' Rework if allowing anonymous posting, for now this is good
							If objLoggedOnUserID > 0 And (objParentPost.ParentThread.HostForum.IsActive) Then
								If (objParentPost.ParentThread.IsClosed = True) Then
									'see if reply is coming from the original thread author
									If objParentPost.ParentThread.StartedByUserID = objLoggedOnUserID Then
										AllowUserEdit = True
									End If
								Else
									' thre forum is active, the post is not closed, user is allowed to post a reply
									AllowUserEdit = True
								End If
							End If

							chkIsPinned.Enabled = False
							chkIsClosed.Enabled = False
					End Select

					If AllowUserEdit = False Then
						HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
					End If

					'Spacer image
					imgAltHeader.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgAltHeaderPreview.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgAltHeaderReply.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgAltHeaderPoll.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")

					imglftHeader.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgrghtHeader.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgReplyLeft.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")
					imgReplyRight.ImageUrl = objConfig.GetThemeImageURL("headfoot_height.gif")

					EnableControls(objAction)

					If objConfig.DisableHTMLPosting = True Then
						teContent.DefaultMode = "BASIC"
						teContent.Mode = "BASIC"
						teContent.ChooseMode = False
						teContent.TextRenderMode = "T"
						teContent.ChooseRender = False
					End If

					GeneratePost(objAction, objForum, objParentPost)

					' See if attachments are enabled
					If objConfig.EnableAttachment Then
						'CP - URL Controller integration
						SetURLController(True)
						If (Security.CanAddAttachments) Then
							SetURLController(True)
						Else
							SetURLController(False)
						End If
					Else
						SetURLController(False)
					End If

					rowNotify.Visible = False
					' Make sure this is a logged on user (shouldn't get here if not logged in anyways)
					If objForumUser.UserID > 0 Then
						' If the user is admin or moderator or trusted
						If (Security.CanLockThread) Then
							' Allow them to lock(close) a thread
							rowClose.Visible = True
						Else
							' Otherwise, don't allow
							rowClose.Visible = False
						End If

						If (Security.CanPinThread) Then
							rowPinned.Visible = True
						Else
							rowPinned.Visible = False
						End If
						' load authorized forums
						Dim objGroups As New GroupController
						Dim arrForums As List(Of ForumInfo)

						For Each objGroup As GroupInfo In objGroups.GroupsGetByModuleID(ModuleId)
							arrForums = objGroup.AuthorizedForums(UserId, True)
							If arrForums.Count > 0 Then
								For Each objForum In arrForums
									ddlForum.Items.Add(New ListItem(objGroup.Name & " - " & objForum.Name, objForum.ForumID.ToString))
								Next
							End If
						Next
						ddlForum.Items.Insert(0, New ListItem("<" & Services.Localization.Localization.GetString("Not_Specified") & ">", "-1"))
						' the option to choose a forum is only available if there is no forumid passed to the module
						If ForumID <> -1 Then
							If Not ddlForum.Items.FindByValue(ForumID.ToString) Is Nothing Then
								ddlForum.Items.FindByValue(ForumID.ToString).Selected = True
							End If
							ddlForum.Enabled = False
						End If

						If objConfig.MailNotification Then
							If Not objForumUser.TrackedModule Then
								' handle Forum tracking
								Dim blnTrackedForum As Boolean = False

								For Each trackedForum As TrackingInfo In objForumUser.TrackedForums
									If trackedForum.ForumID = ForumID Then
										blnTrackedForum = True
										Exit For
									End If
								Next

								If (Not blnTrackedForum) Then
									Dim blnTrackedThread As Boolean = False
									' Forum is NOT already being tracked, possibly show user the option to subscribe
									' we need to check the case to see how to handle tracking at the thread level
									Select Case objAction
										Case PostAction.Edit
											' user may already be tracking the thread
											' we may not have threadID, we definately have postid
											For Each trackedThread As TrackingInfo In objForumUser.TrackedThreads
												If trackedThread.ThreadID = objParentPost.ThreadID Then
													blnTrackedThread = True
													Exit For
												End If
											Next
										Case PostAction.[New]
											' Do nothing, its a new thread and impossible to track
										Case Else	  ' reply/quote
											' user may already be tracking the thread
											' we may not have threadID, we definately have postid
											For Each trackedThread As TrackingInfo In objForumUser.TrackedThreads
												If trackedThread.ThreadID = objParentPost.ThreadID Then
													blnTrackedThread = True
													Exit For
												End If
											Next
									End Select
									' If the user is not already tracking the thread, give them the option
									If (Not blnTrackedThread) Then
										rowNotify.Visible = True
									End If
								End If
							Else
								' user is tracking subscriptions at the module level (not implemented)
								rowNotify.Visible = True
							End If
						End If
						'Display Emoticons?
						rowEmoticon.Visible = objConfig.EnableEmoticons
					End If

					If Not Request.UrlReferrer Is Nothing Then
						' Store URL Referrer to return to portal
						ViewState("UrlReferrer") = Request.UrlReferrer.ToString()
					End If
					' Set the form focus to the subject textbox(avoid users missing subject)
					SetFormFocus(txtSubject)
				End If
			Catch exc As Exception
				ProcessModuleLoadException(Me, exc)
			End Try
		End Sub

		''' <summary>
		''' This submites the post from either initial view or preview view.  
		''' This calls to PostToDataBase function which then takes the necessary action
		''' and then navigates the user to the appropriate screen.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>ASP.NET 2.0 apparently does not allow two items to be handled by a single event. 
		''' </remarks>
		Protected Sub cmdSubmit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSubmit.Click
			Try
				If Page.IsValid Then
					Dim objLoggedOnUserID As Integer = -1

					Dim ParentPostID As Integer = 0
					Dim PostID As Integer = -1
					Dim PollID As Integer = -1
					Dim ThreadIconID As Integer = -1 ' NOT IMPLEMENTED
					Dim RemoteAddress As String = "0.0.0.0"
					Dim ThreadStatus As Forum.ThreadStatus
					Dim URLPostID As Integer = -1
					'Dim IsModerated As Boolean = False
					Dim IsQuote As Boolean = False

					If Request.IsAuthenticated = False Then
						' CP - Consider a way to save subject, body, etc so user can comeback and post if they timed out.
						HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
						Exit Sub
					Else
						objLoggedOnUserID = Users.UserController.GetCurrentUserInfo.UserID
					End If

					Dim objForumUser As ForumUser = ForumUserController.GetForumUser(objLoggedOnUserID, False, ModuleId, PortalId)
					Dim objAction As New PostAction

					' Validation (from UI)
					If Len(teContent.Text) = 0 Then
						lblInfo.Text = Localization.GetString(Forum.PostMessage.PostInvalidBody.ToString() + ".Text", LocalResourceFile)
						lblInfo.Visible = True
						Exit Sub
					End If
					If Len(txtSubject.Text) = 0 Then
						lblInfo.Text = Localization.GetString(Forum.PostMessage.PostInvalidSubject.ToString() + ".Text", LocalResourceFile)
						lblInfo.Visible = True
						Exit Sub
					End If
					If ddlForum.SelectedItem Is Nothing Or ddlForum.SelectedItem.Value = "-1" Then
						lblInfo.Text = Localization.GetString(Forum.PostMessage.ForumDoesntExist.ToString() + ".Text", LocalResourceFile)
						lblInfo.Visible = True
						Exit Sub
					End If

					If (Not Request.QueryString("action") Is Nothing) Then
						objAction = CType([Enum].Parse(GetType(PostAction), Request.QueryString("action"), True), PostAction)
					End If

					If Not Request.QueryString("postid") Is Nothing Then
						URLPostID = Integer.Parse(Request.QueryString("postid"))
					End If

					Dim objForum As ForumInfo = ForumInfo.GetForumInfo(Integer.Parse(ddlForum.SelectedItem.Value))
					Dim objModSecurity As New Forum.ModuleSecurity(ModuleId, TabId, objForum.ForumID, objLoggedOnUserID)

					Select Case objAction
						Case PostAction.Edit
							Dim cntPost As New PostController
							Dim objEditPost As New PostInfo

							objEditPost = cntPost.PostGet(URLPostID, PortalId)

							ParentPostID = objEditPost.ParentPostID
							PostID = objEditPost.PostID

							' if polls are enabled, make sure db is properly setup
							If objForum.AllowPolls And PollID > 0 Then
								If Not HandlePoll(PollID, False) Then
									Exit Sub
								End If
							End If
						Case PostAction.New
							' not sure this is correct (first line below)
							ParentPostID = URLPostID

							If objForum.AllowPolls And PollID > 0 Then
								If Not HandlePoll(PollID, False) Then
									Return
								End If
							ElseIf objForum.AllowPolls Then
								OrphanPollCleanup()
							End If
						Case PostAction.Quote
							Dim cntPost As New PostController
							Dim objReplyToPost As New PostInfo

							objReplyToPost = cntPost.PostGet(URLPostID, PortalId)

							ParentPostID = URLPostID
							IsQuote = True
						Case Else	  ' reply
							Dim cntPost As New PostController
							Dim objReplyToPost As New PostInfo

							objReplyToPost = cntPost.PostGet(URLPostID, PortalId)

							ParentPostID = URLPostID
					End Select

					If ParentPostID = 0 And objForum.AllowPolls Then
						PollID = CType(txtPollID.Text, Integer)
					End If

					If Not Request.ServerVariables("REMOTE_ADDR") Is Nothing Then
						RemoteAddress = Request.ServerVariables("REMOTE_ADDR")
					End If

					If ddlThreadStatus.SelectedIndex > 0 Then
						ThreadStatus = CType(ddlThreadStatus.SelectedValue, Forum.ThreadStatus)
					Else
						ThreadStatus = Forum.ThreadStatus.NotSet
					End If

					Dim cntPostConnect As New PostConnector
					Dim PostMessage As PostMessage

					PostMessage = cntPostConnect.SubmitInternalPost(TabId, ModuleId, PortalId, objLoggedOnUserID, txtSubject.Text, teContent.Text, objForum.ForumID, ParentPostID, PostID, chkIsPinned.Checked, chkIsClosed.Checked, chkNotify.Checked, ThreadStatus, ctlAttachment.lstAttachmentIDs, RemoteAddress, PollID, ThreadIconID, IsQuote)

					Select Case PostMessage
						Case PostMessage.PostApproved
							Dim ReturnURL As String = NavigateURL()

							If objModSecurity.IsModerator Then
								If Not ViewState("UrlReferrer") Is Nothing Then
									ReturnURL = (CType(ViewState("UrlReferrer"), String))
								Else
									ReturnURL = Utilities.Links.ContainerViewForumLink(TabId, objForum.ForumID, False)
								End If
							Else
								If Not objAction = PostAction.New Then
									ReturnURL = Utilities.Links.ContainerViewPostLink(TabId, objForum.ForumID, URLPostID)
								Else
									ReturnURL = Utilities.Links.ContainerViewForumLink(TabId, objForum.ForumID, False)
								End If
							End If

							Response.Redirect(ReturnURL, False)
						Case PostMessage.PostModerated
							tblNewPost.Visible = False
							tblOldPost.Visible = False
							tblPreview.Visible = False
							cmdCancel.Visible = False
							cmdBackToEdit.Visible = False
							cmdSubmit.Visible = False
							cmdPreview.Visible = False
							cmdBackToForum.Visible = True
							rowModerate.Visible = True
							tblPoll.Visible = False
						Case Else
							lblInfo.Visible = True
							lblInfo.Text = Localization.GetString(PostMessage.ToString() + ".Text", LocalResourceFile)
					End Select
				Else
					lblInfo.Visible = True
					Exit Sub
				End If
			Catch exc As Exception
				ProcessModuleLoadException(Me, exc)
			End Try
		End Sub

		''' <summary>
		''' Takes the user back to where they were.
		''' Handle any poll cleanup, just in case.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		Protected Sub cmdCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancel.Click
			Try
				Dim objForum As ForumInfo = ForumInfo.GetForumInfo(Integer.Parse(ddlForum.SelectedItem.Value))

				If objForum.AllowPolls Then
					' Make sure user didn't create poll here that they are about to orhpan
					If txtPollID.Text <> String.Empty Then
						Dim cntPoll As New PollController
						Dim objPoll As New PollInfo

						objPoll = cntPoll.GetPoll(CType(txtPollID.Text, Integer))

						If Not objPoll Is Nothing Then
							If objPoll.ThreadID = -1 Then
								' No thread assigned to the poll, delete it
								cntPoll.DeletePoll(CType(txtPollID.Text, Integer))
							Else
								' thread exists, need to make sure we still have valid # of answers
								If Not HandlePoll(CType(txtPollID.Text, Integer), True) Then
									Exit Sub
								End If
							End If
						End If
					End If
					' cleanup any other oprhans
					OrphanPollCleanup()
				End If
				If Not ViewState("UrlReferrer") Is Nothing Then
					Response.Redirect(CType(ViewState("UrlReferrer"), String), False)
				End If
			Catch exc As Exception
				ProcessModuleLoadException(Me, exc)
			End Try
		End Sub

		''' <summary>
		''' Hides areas on the screen and shows user what their post will look like
		''' once it is submitted.  (This does rendering)
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		Protected Sub cmdPreview_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdPreview.Click
			Try
				' Obtain post action
				Dim objAction As PostAction
				If (Not Request.QueryString("action") Is Nothing) Then
					objAction = CType([Enum].Parse(GetType(PostAction), Request.QueryString("action"), True), PostAction)
				End If

				Dim Connect As New PostConnector
				lblPreview.Text = Connect.ProcessPostBody(teContent.Text, objConfig, PortalId, objAction, UserId)

				tblPreview.Visible = True
				cmdSubmit.Visible = True
				cmdPreview.Visible = False
				cmdCancel.Visible = False
				cmdBackToEdit.Visible = True
				tblNewPost.Visible = False
				lblNoAnswer.Visible = False

				If tblOldPost.Visible = True Then
					ViewState("DisplayOldPost") = "True"
					tblOldPost.Visible = False
				End If

				If tblPoll.Visible = True Then
					ViewState("DisplayPoll") = "True"
					tblPoll.Visible = False
				End If

			Catch exc As Exception
				ProcessModuleLoadException(Me, exc)
			End Try
		End Sub

		''' <summary>
		''' Takes the user back to edit post mode when clicked.  
		''' (This is only visible in preview mode)
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		Protected Sub cmdBackToEdit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdBackToEdit.Click
			tblPreview.Visible = False
			cmdCancel.Visible = True
			cmdSubmit.Visible = True
			cmdPreview.Visible = True
			cmdBackToEdit.Visible = False
			tblNewPost.Visible = True

			If Not ViewState("DisplayOldPost") Is Nothing Then
				tblOldPost.Visible = True
			End If
			If Not ViewState("DisplayPoll") Is Nothing Then
				tblPoll.Visible = True
			End If

		End Sub

		''' <summary>
		''' For moderated post, redirect back to the forum threads page
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks>
		''' </remarks>
		Protected Sub cmdBackToForum_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdBackToForum.Click
			Try
				If Not ViewState("UrlReferrer") Is Nothing Then
					Response.Redirect(CType(ViewState("UrlReferrer"), String), False)
				Else
					Response.Redirect(NavigateURL(), False)
				End If
			Catch exc As Exception
				ProcessModuleLoadException(Me, exc)
			End Try
		End Sub

		''' <summary>
		''' Adds an answer to the available options for a poll.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks></remarks>
		Protected Sub cmdAddAnswer_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAddAnswer.Click
			lblInfo.Visible = False
			ddlThreadStatus.Enabled = False
			ApplyAnswerOrder()
			AddPollAnswer()
			txtAddAnswer.Text = String.Empty
		End Sub

		''' <summary>
		''' Fired off when the thread status is changed to change what is available to the poster.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks></remarks>
		Protected Sub ddlThreadStatus_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlThreadStatus.SelectedIndexChanged
			If ddlThreadStatus.SelectedValue = CInt(ThreadStatus.Poll).ToString() Then
				tblPoll.Visible = True
			Else
				tblPoll.Visible = False
			End If
		End Sub

		''' <summary>
		''' dgAnswers_ItemCommand runs when a Command event is raised in the Grid 
		''' </summary>
		''' <remarks>Only if DHTML is not supported</remarks>
		Protected Sub dgAnswers_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles dgAnswers.ItemCommand
			Dim commandName As String = e.CommandName
			Dim commandArgument As Integer = CType(e.CommandArgument, Integer)

			Select Case commandName
				Case "Delete"
					Dim AnswerID As Integer = CType(dgAnswers.DataKeys(e.Item.ItemIndex), Integer)
					DeleteAnswer(AnswerID)
				Case "MoveUp"
					Dim index As Integer = e.Item.ItemIndex
					MoveAnswerUp(index)
				Case "MoveDown"
					Dim index As Integer = e.Item.ItemIndex
					MoveAnswerDown(index)
			End Select
		End Sub

		''' <summary>
		''' When it is determined that the client supports a rich interactivity the dgdgAnswers_ItemCreated 
		''' event is responsible for disabling all the unneeded AutoPostBacks, along with assiging the appropriate
		'''	client-side script for each event handler
		''' </summary>
		''' <remarks>
		''' </remarks>
		Protected Sub dgAnswers_ItemCreated(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles dgAnswers.ItemCreated
			If SupportsRichClient() Then
				Select Case e.Item.ItemType

					Case ListItemType.AlternatingItem, ListItemType.Item
						DotNetNuke.UI.Utilities.ClientAPI.EnableClientSideReorder(e.Item.Cells(COLUMN_MOVE_DOWN).Controls(0), Me.Page, False, Me.dgAnswers.ClientID)
						DotNetNuke.UI.Utilities.ClientAPI.EnableClientSideReorder(e.Item.Cells(COLUMN_MOVE_UP).Controls(0), Me.Page, True, Me.dgAnswers.ClientID)
				End Select
			End If
		End Sub

		''' <summary>
		''' Modifies items as they are bound to the datagrid.
		''' </summary>
		''' <param name="sender"></param>
		''' <param name="e"></param>
		''' <remarks></remarks>
		Protected Sub dgAnswers_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles dgAnswers.ItemDataBound
			Dim item As System.Web.UI.WebControls.DataGridItem = e.Item

			If item.ItemType = System.Web.UI.WebControls.ListItemType.Item Or _
			    item.ItemType = System.Web.UI.WebControls.ListItemType.AlternatingItem Or _
			    item.ItemType = System.Web.UI.WebControls.ListItemType.SelectedItem Then
			End If
		End Sub

#End Region

#Region "Private Methods"

#Region "Answers Grid"

		''' <summary>
		''' Removes an answer from a poll.
		''' </summary>
		''' <param name="AnswerID">The Answer to delete.</param>
		''' <remarks></remarks>
		Private Sub DeleteAnswer(ByVal AnswerID As Integer)
			' '' Make sure answers are in proper order.
			'ApplyAnswerOrder()
			Dim cntAnswers As New AnswerController
			cntAnswers.DeleteAnswer(AnswerID)

			BindGrid()
		End Sub

		''' <summary>
		''' Adds an answer to a poll, if the poll doesn't exist it gets created.
		''' </summary>
		''' <remarks></remarks>
		Private Sub AddPollAnswer()
			If txtAddAnswer.Text <> String.Empty Then
				lblNoAnswer.Visible = False
				Dim objSecurity As New PortalSecurity
				Dim ctlWordFilter As New WordFilterController
				Dim PollQuestion As String
				Dim PollTakenMessage As String

				If txtPollID.Text = "-1" Then
					' Poll doesn't exist yet
					Dim cntPoll As New PollController
					Dim objPoll As New PollInfo

					If txtQuestion.Text <> String.Empty Then
						PollQuestion = objSecurity.InputFilter(txtQuestion.Text, PortalSecurity.FilterFlag.NoScripting)
					Else
						PollQuestion = objSecurity.InputFilter(Localization.GetString("DefaultNewPoll", Me.LocalResourceFile), PortalSecurity.FilterFlag.NoScripting)
					End If
					txtQuestion.Text = PollQuestion

					If txtTakenMessage.Text <> String.Empty Then
						PollTakenMessage = objSecurity.InputFilter(txtTakenMessage.Text, PortalSecurity.FilterFlag.NoScripting)
					Else
						PollTakenMessage = objSecurity.InputFilter(Localization.GetString("DefaultTakenMsg", Me.LocalResourceFile), PortalSecurity.FilterFlag.NoScripting)
					End If
					txtTakenMessage.Text = PollTakenMessage

					If objConfig.EnableBadWordFilter Then
						PollQuestion = ctlWordFilter.FilterBadWord(PollQuestion, PortalId)
						PollTakenMessage = ctlWordFilter.FilterBadWord(PollTakenMessage, PortalId)
					End If
					objPoll.Question = PollQuestion
					objPoll.TakenMessage = PollTakenMessage

					objPoll.ShowResults = chkShowResults.Checked
					If txtEndDate.Text <> String.Empty Then
						objPoll.EndDate = CDate(txtEndDate.Text)
					End If

					objPoll.ShowResults = chkShowResults.Checked


					objPoll.ModuleID = ModuleId
					Dim PollID As Integer = -1
					PollID = cntPoll.AddPoll(objPoll)
					txtPollID.Text = PollID.ToString()

					' Now we have a poll, create the first answer
					Dim cntAnswer As New AnswerController
					Dim objAnswer As New AnswerInfo
					objAnswer.PollID = PollID
					objAnswer.SortOrder = 0
					objAnswer.Answer = objSecurity.InputFilter(txtAddAnswer.Text, PortalSecurity.FilterFlag.NoScripting)

					cntAnswer.AddAnswer(objAnswer)

					BindGrid()
				Else
					' Poll exists already
					' First make sure we update the sort order in case the user changed it

					' Now add the answer item
					Dim cntAnswer As New AnswerController
					Dim objAnswer As New AnswerInfo
					objAnswer.PollID = CType(txtPollID.Text, Integer)
					objAnswer.SortOrder = dgAnswers.Items.Count
					objAnswer.Answer = objSecurity.InputFilter(txtAddAnswer.Text, PortalSecurity.FilterFlag.NoScripting)

					cntAnswer.AddAnswer(objAnswer)

					BindGrid()
				End If
			Else
				' show some warning/error
				lblNoAnswer.Visible = True
			End If
		End Sub

		''' <summary>
		''' Persists sort order changes to the database.
		''' </summary>
		''' <param name="objAnswer"></param>
		''' <remarks></remarks>
		Private Sub UpdateAnswerSortOrder(ByVal objAnswer As AnswerInfo)
			Dim cntAnswers As New AnswerController

			cntAnswers.UpdateAnswer(objAnswer)
		End Sub

		''' <summary>
		''' Takes the client side grid and gets the new sort order.
		''' </summary>
		''' <remarks></remarks>
		Private Sub ApplyAnswerOrder()
			Try
				Dim cntAnswers As New AnswerController
				Dim arrAnswers As List(Of AnswerInfo)
				arrAnswers = cntAnswers.GetPollAnswers(CType(txtPollID.Text, Integer))

				Dim aryNewOrder() As String = DotNetNuke.UI.Utilities.ClientAPI.GetClientSideReorder(Me.dgAnswers.ClientID, Me.Page)
				'assign sortorder
				For i As Integer = 0 To aryNewOrder.Length - 1
					arrAnswers(CInt(aryNewOrder(i))).SortOrder = i
					UpdateAnswerSortOrder(arrAnswers(CInt(aryNewOrder(i))))
				Next
				BindGrid()
			Catch ex As Exception
				LogException(ex)
			End Try
		End Sub

		''' <summary>
		''' Binds a list of available answers to the poll answers grid.
		''' </summary>
		''' <remarks></remarks>
		Private Sub BindGrid()
			If Not txtPollID.Text = "-1" Then
				Dim cntAnswers As New AnswerController
				Dim arrAnswers As New List(Of AnswerInfo)

				arrAnswers = cntAnswers.GetPollAnswers(CType(txtPollID.Text, Integer))
				If arrAnswers.Count > 0 Then
					dgAnswers.DataKeyField = "AnswerID"
					dgAnswers.DataSource = arrAnswers
					dgAnswers.DataBind()
					dgAnswers.ShowHeader = True
				Else
					dgAnswers.DataSource = New ArrayList()
					dgAnswers.DataBind()
					dgAnswers.ShowHeader = False
				End If
			End If
		End Sub

		''' <summary>
		''' Moves an Answer  (Only used when DHTML is not supported)
		''' </summary>
		''' <param name="index">The index of the Answer to move.</param>
		''' <param name="destIndex">The new index of the Answer.</param>
		''' <history>
		''' </history>
		Private Sub MoveAnswer(ByVal index As Integer, ByVal destIndex As Integer)
			Dim cntAnswers As New AnswerController
			Dim arrAnswers As List(Of AnswerInfo)
			arrAnswers = cntAnswers.GetPollAnswers(CType(txtPollID.Text, Integer))

			Dim objAnswer As AnswerInfo = arrAnswers(index)
			Dim objNext As AnswerInfo = arrAnswers(destIndex)

			Dim currentOrder As Integer = objAnswer.SortOrder
			Dim nextOrder As Integer = objNext.SortOrder
			'Swap ViewOrders
			objAnswer.SortOrder = nextOrder
			objNext.SortOrder = currentOrder
			'Refresh Grid
			BindGrid()
		End Sub

		''' <summary>
		''' Moves an Answer down in the SortOrder.
		''' </summary>
		''' <param name="index">The index of the Answer to move.</param>
		''' <history>
		''' </history>
		Private Sub MoveAnswerDown(ByVal index As Integer)
			MoveAnswer(index, index + 1)
		End Sub

		''' <summary>
		''' Moves an Answer up in the SortOrder.
		''' </summary>
		''' <param name="index">The index of the Answer to move.</param>
		''' <history>
		''' </history>
		Private Sub MoveAnswerUp(ByVal index As Integer)
			MoveAnswer(index, index - 1)
		End Sub

		''' <summary>
		''' Helper function that determines whether the client-side functionality is possible
		''' </summary>
		''' <returns></returns>
		''' <remarks></remarks>
		Private Function SupportsRichClient() As Boolean
			Return DotNetNuke.UI.Utilities.ClientAPI.BrowserSupportsFunctionality(DotNetNuke.UI.Utilities.ClientAPI.ClientFunctionality.DHTML)
		End Function

#End Region

		''' <summary>
		''' Used to make sure polls have enough answers, if not they are to be removed.
		''' </summary>
		''' <param name="PollID"></param>
		''' <remarks></remarks>
		Private Function HandlePoll(ByVal PollID As Integer, ByVal Cancel As Boolean) As Boolean
			Dim boolContinue As Boolean = True
			Dim objSecurity As New PortalSecurity

			If dgAnswers.Items.Count > 1 Then
				lblInfo.Visible = False
				Dim cntPoll As New PollController
				Dim objPoll As New PollInfo
				Dim ctlWordFilter As New WordFilterController
				Dim PollQuestion As String
				Dim PollTakenMessage As String

				objPoll.PollID = PollID

				PollQuestion = objSecurity.InputFilter(txtQuestion.Text, PortalSecurity.FilterFlag.NoScripting)
				PollTakenMessage = objSecurity.InputFilter(txtTakenMessage.Text, PortalSecurity.FilterFlag.NoScripting)
				If objConfig.EnableBadWordFilter Then
					PollQuestion = ctlWordFilter.FilterBadWord(PollQuestion, PortalId)
					PollTakenMessage = ctlWordFilter.FilterBadWord(PollTakenMessage, PortalId)
				End If
				objPoll.Question = PollQuestion
				objPoll.TakenMessage = PollTakenMessage

				objPoll.ShowResults = chkShowResults.Checked
				If txtEndDate.Text <> String.Empty Then
					objPoll.EndDate = CDate(txtEndDate.Text)
				End If

				cntPoll.UpdatePoll(objPoll)
				' make sure answer sort order is handled.
				If Not Cancel Then
					ApplyAnswerOrder()
				End If
			ElseIf dgAnswers.Items.Count = 1 Then
				' show user they need more than a single answer for the poll
				lblInfo.Visible = True
				lblInfo.Text = Localization.GetString("lblMoreAnswers", LocalResourceFile)

				boolContinue = False
			Else
				' no answers for poll, delete it (first make sure thread status is not set to poll)
				If ddlThreadStatus.SelectedValue = CInt(ThreadStatus.Poll).ToString() Then
					ddlThreadStatus.SelectedValue = CInt(ThreadStatus.NotSet).ToString()
				End If
				' answers removed, delete poll
				Dim cntPoll As New PollController
				cntPoll.DeletePoll(PollID)
				' make sure poll is no longer associated w/ post
				PollID = -1
			End If
			Return boolContinue
		End Function

		''' <summary>
		''' Removes orphaned polls from the database.
		''' </summary>
		''' <remarks></remarks>
		Private Sub OrphanPollCleanup()
			' Forum allows polls, cleanup any orphans (which should very rarely occur) - only way to have orphan is poll creation, never creating thread
			Dim cntPoll As New PollController
			Dim arrPolls As List(Of PollInfo)
			arrPolls = cntPoll.GetOrphanedPolls(ModuleId)

			If arrPolls.Count > 0 Then
				For Each objPoll As PollInfo In arrPolls
					cntPoll.DeletePoll(objPoll.PollID)
				Next
			End If

			ddlThreadStatus.SelectedValue = CInt(ThreadStatus.NotSet).ToString()
		End Sub

		''' <summary>
		''' Sets the properties of the core's URLControl (used for attachments)
		''' </summary>
		''' <param name="Enabled">Boolean which signifies if attachments are enabled or not.</param>
		''' <remarks></remarks>
		Private Sub SetURLController(ByVal Enabled As Boolean)
			If Enabled Then
				rowAttachments.Visible = True
				'We only set the PostID if we are editing a post!
				If Not Request.QueryString("action") Is Nothing Then
					If Request.QueryString("action").ToLower = "edit" Then
						ctlAttachment.PostId = Int32.Parse(Request.QueryString("postid"))
					Else
						ctlAttachment.PostId = -1
					End If
				Else
					ctlAttachment.PostId = -1
				End If

				'AJAX
				ctlAttachment.LoadInitialView()
			Else
				ctlAttachment.Visible = False
				rowAttachments.Visible = False
			End If
		End Sub

		''' <summary>
		''' Show/Hides replied to post if it exists. Also sets the intial visiblity for this screen.
		''' </summary>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	11/28/2005	Created
		''' </history>
		Private Sub EnableControls(ByVal objAction As PostAction)
			If (objAction = PostAction.Reply) Then
				tblOldPost.Visible = True
			Else
				tblOldPost.Visible = False
			End If

			cmdBackToForum.Visible = False
			cmdBackToEdit.Visible = False
			cmdCancel.Visible = True
			cmdSubmit.Visible = True
			rowModerate.Visible = False
		End Sub

		''' <summary>
		''' This gets the data for the post if in edit post mode.
		''' </summary>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	11/28/2005	Created
		''' </history>
		Private Sub GeneratePost(ByVal objAction As PostAction, ByVal objForum As ForumInfo, ByVal objParentPost As PostInfo)
			Dim ctlForum As New ForumController
			' generate post content
			If (Not objAction = PostAction.[New]) Then
				' [skeel] no need to do anything here unless reply or edit
				Dim fTextDecode As Utilities.PostContent = Nothing
				If objAction = PostAction.Edit Or objAction = PostAction.Reply Then
					If objParentPost.ParseInfo = PostParserInfo.None Or objParentPost.ParseInfo = PostParserInfo.File Then
						'Nothing to Parse or just an Attachment not inline
						fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig)
					Else
						If objParentPost.ParseInfo < PostParserInfo.Inline Then
							'Something to parse, but not any inline instances
							If objConfig.DisableHTMLPosting = True And objAction = PostAction.Edit Then
								' We are editing with HTML disabled, don't parse anything!
								fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig)
							Else
								If objAction = PostAction.Edit Then
									'For editing, we only parse BBcode
									fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig, PostParserInfo.BBCode)
								Else
									fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig, objParentPost.ParseInfo)
								End If
							End If
						Else
							'At lease Inline to Parse
							If objAction = PostAction.Edit Then
								If objConfig.DisableHTMLPosting = True Then
									' We never parse for editing of HTML disabled posts
									fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig)
								Else
									'Do the BBCode, we are editing
									fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig, PostParserInfo.BBCode)
								End If
							ElseIf objAction = PostAction.Reply Then
								'Ignore the inlines, this is a parentpost
								fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig, objParentPost.ParseInfo, objParentPost.Attachments, True)
							End If
						End If
					End If
				ElseIf objAction = PostAction.Quote Then
					If objConfig.DisableHTMLPosting = True Then
						' We don't parse quotes either when HTML is disabled
						fTextDecode = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(objParentPost.Body), objConfig)
					Else
						' When quoting a post, we should restrict to only parsing of BBcode and emoticons
						Dim strQuoteBody As String = System.Web.HttpUtility.HtmlDecode(objParentPost.Body)
						' do we need to replace inline attachments?
						If objParentPost.ParseInfo >= PostParserInfo.Inline Then
							strQuoteBody = Utilities.ForumUtils.RemoveInlineAttachments(strQuoteBody)
						End If
						fTextDecode = New Utilities.PostContent(strQuoteBody, objConfig, PostParserInfo.BBCode + PostParserInfo.Emoticon)
					End If
				End If

				With objParentPost
					chkNotify.Checked = .Notify
					chkIsClosed.Checked = .ParentThread.IsClosed
					chkIsPinned.Checked = .ParentThread.IsPinned
				End With

				hlAuthor.Text = objParentPost.Author.SiteAlias
				hlAuthor.NavigateUrl = Utilities.Links.UserPublicProfileLink(TabId, ModuleId, objParentPost.UserID)
				hlAuthor.ToolTip = Localization.GetString("ReplyToToolTip", Me.LocalResourceFile)

				If objAction = PostAction.Reply Then
					lblMessage.Text = fTextDecode.Text
				End If

				Select Case objAction
					Case PostAction.Edit
						txtSubject.Text = HttpUtility.HtmlDecode(objParentPost.Subject)
						teContent.Text = fTextDecode.Text
						BindThreadStatus()

						If objConfig.EnableThreadStatus And objParentPost.ParentPostID = 0 And objForum.EnableForumsThreadStatus Then
							rowThreadStatus.Visible = True
							ddlThreadStatus.SelectedIndex = CType(objParentPost.ParentThread.ThreadStatus, Integer)
						Else
							If objForum.AllowPolls And objParentPost.ParentPostID = 0 Then
								ddlThreadStatus.SelectedIndex = CType(objParentPost.ParentThread.ThreadStatus, Integer)
							End If
							rowThreadStatus.Visible = False
						End If

						' handle if editing first post, show polling options if enabled
						If objForum.AllowPolls And objParentPost.ParentPostID = 0 And ddlThreadStatus.SelectedValue = CInt(ThreadStatus.Poll).ToString() Then
							tblPoll.Visible = True
							txtPollID.Text = objParentPost.ParentThread.PollID.ToString()
							Dim cntPoll As New PollController
							Dim objPoll As New PollInfo

							objPoll = cntPoll.GetPoll(objParentPost.ParentThread.PollID)

							If Not objPoll Is Nothing Then
								txtQuestion.Text = objPoll.Question
								txtTakenMessage.Text = objPoll.TakenMessage
								chkShowResults.Checked = objPoll.ShowResults
								txtEndDate.Text = objPoll.EndDate.ToShortDateString()
								' Handle existing poll information
								BindGrid()
							End If
						Else
							tblPoll.Visible = False
						End If
					Case PostAction.Reply
						tblPoll.Visible = False
						txtSubject.Text = HttpUtility.HtmlDecode(ReplySubject(objParentPost))
						rowThreadStatus.Visible = False
					Case PostAction.Quote
						tblPoll.Visible = False
						txtSubject.Text = HttpUtility.HtmlDecode(ReplySubject(objParentPost))
						teContent.Text = fTextDecode.ProcessQuoteBody(objParentPost.Author.SiteAlias, objConfig)
						rowThreadStatus.Visible = False
				End Select
			Else
				' This is a new thread
				If objConfig.EnableThreadStatus And objForum.EnableForumsThreadStatus Then
					BindThreadStatus()
					rowThreadStatus.Visible = True
					tblPoll.Visible = False
				Else
					' Either module or forum doesn't use thread status, handle showing of polling options if enabled
					If objForum.AllowPolls Then
						tblPoll.Visible = True
					Else
						tblPoll.Visible = False
					End If
					rowThreadStatus.Visible = False
				End If
			End If
		End Sub

		'''' <summary>
		'''' Gets the URL to return the posting user too.
		'''' </summary>
		'''' <param name="Post"></param>
		'''' <returns></returns>
		'''' <remarks></remarks>
		'Private Function GetApprovedPostReturnURL(ByVal Post As PostInfo) As String
		'	Dim url As String

		'	' This only needs to be handled for moderators (which are trusted by no matter what, so it only happens at this point)
		'	Dim Security As New Forum.ModuleSecurity(ModuleId, TabId, Post.ForumID, UserId)

		'	If Security.IsModerator Then
		'		If Not ViewState("UrlReferrer") Is Nothing Then
		'			url = (CType(ViewState("UrlReferrer"), String))
		'		Else
		'			' behave as before (normal usage)
		'			url = Utils.ContainerViewPostLink(TabId, Post.ForumID, Post.ThreadID, Post.PostID)
		'		End If
		'	Else
		'		' behave as before (normal usage)
		'		url = Utils.ContainerViewPostLink(TabId, Post.ForumID, Post.ThreadID, Post.PostID)
		'	End If

		'	Return url
		'End Function

		''' <summary>
		''' Gets the subject if this is a quote or reply and pre-populates that
		''' text box.
		''' </summary>
		''' <returns></returns>
		''' <remarks>
		''' </remarks>
		''' <history>
		''' 	[cpaterra]	11/28/2005	Created
		''' </history>
		Private Function ReplySubject(ByVal objParentPost As PostInfo) As String
			Dim strSubject As String = objParentPost.Subject

			If (strSubject.Length >= 3) Then
				If Not (strSubject.Substring(0, 3) = "Re:") Then
					strSubject = "Re: " + strSubject
				End If
			Else
				strSubject = "Re: " + strSubject
			End If
			Return strSubject
		End Function

		''' <summary>
		''' Binds the list of available thread status choices.
		''' </summary>
		''' <remarks></remarks>
		Private Sub BindThreadStatus()
			Dim ctlLists As New DotNetNuke.Common.Lists.ListController
			Dim colThreadStatus As DotNetNuke.Common.Lists.ListEntryInfoCollection = ctlLists.GetListEntryInfoCollection("ThreadStatus")
			ddlThreadStatus.Items.Clear()

			For Each entry As DotNetNuke.Common.Lists.ListEntryInfo In colThreadStatus
				Dim statusEntry As New ListItem(Localization.GetString(entry.Text, objConfig.SharedResourceFile), entry.Value)
				ddlThreadStatus.Items.Add(statusEntry)
			Next

			'polling changes
			Try
				Dim ForumID As Integer

				If Not Request.QueryString("forumid") Is Nothing Then
					ForumID = CInt(Request.QueryString("forumid"))
				End If

				Dim objForum As ForumInfo = ForumInfo.GetForumInfo(ForumID)

				If objForum.AllowPolls Then
					Dim statusEntry As New ListItem(Localization.GetString("Poll", objConfig.SharedResourceFile), CInt(ThreadStatus.Poll).ToString())
					ddlThreadStatus.Items.Add(statusEntry)
				End If
			Catch ex As Exception

			End Try
		End Sub

#End Region

	End Class

End Namespace
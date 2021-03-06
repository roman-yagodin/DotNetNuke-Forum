'
' DotNetNukeŽ - http://www.dotnetnuke.com
' Copyright (c) 2002-2011
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

Imports DotNetNuke.Entities.Users
Imports DotNetNuke.Services.FileSystem
Imports DotNetNuke.Forum.Library

Namespace DotNetNuke.Modules.Forum

    ''' <summary>
    ''' Renders the Posts view UI.  
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Public Class Posts
        Inherits ForumObject

#Region "Private Members"

        Private _ThreadID As Integer
        Private _PostCollection As List(Of PostInfo)
        Private _objThread As ThreadInfo
        Private _PostPage As Integer = 0
        Private _TrackedForum As Boolean = False
        Private _TrackedThread As Boolean = False

#Region "Controls"

        Private trcRating As Telerik.Web.UI.RadRating
        Private ddlViewDescending As DotNetNuke.Web.UI.WebControls.DnnComboBox
        Private chkEmail As CheckBox
        Private ddlThreadStatus As DotNetNuke.Web.UI.WebControls.DnnComboBox
        Private cmdThreadAnswer As LinkButton
        Private txtForumSearch As TextBox
        Private cmdForumSearch As ImageButton
        Private hsThreadAnswers As New Hashtable
        Private rblstPoll As RadioButtonList
        Private cmdVote As LinkButton
        Private cmdBookmark As ImageButton
        Private tagsControl As DotNetNuke.Web.UI.WebControls.Tags
        Private txtQuickReply As TextBox
        Private cmdSubmit As LinkButton
        Private cmdThreadSubscribers As LinkButton

#End Region

        ''' <summary>
        ''' This is used to determine the permissions for the current user/forum combination. 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private ReadOnly Property objSecurity() As ModuleSecurity
            Get
                Return New ModuleSecurity(ModuleID, TabID, ForumID, CurrentForumUser.UserID)
            End Get
        End Property

        ''' <summary>
        ''' The PostID being rendered, if the user was not directed here via a postid, the threadid is used.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private ReadOnly Property PostID() As Integer
            Get
                If HttpContext.Current.Request.QueryString("postid") IsNot Nothing Then
                    Return Convert.ToInt32(HttpContext.Current.Request.QueryString("postid"))
                Else
                    Return -1
                End If
            End Get
        End Property

        ''' <summary>
        ''' The ThreadID for all the posts being rendered.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property ThreadID() As Integer
            Get
                If HttpContext.Current.Request.QueryString("threadid") IsNot Nothing Then
                    Return Convert.ToInt32(HttpContext.Current.Request.QueryString("threadid"))
                Else
                    Return _ThreadID
                End If
            End Get
            Set(ByVal Value As Integer)
                _ThreadID = Value
            End Set
        End Property

        ''' <summary>
        ''' The ThreadInfo object of the ThreadID being rendered.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property objThread() As ThreadInfo
            Get
                Return _objThread
            End Get
            Set(ByVal Value As ThreadInfo)
                _objThread = Value
            End Set
        End Property

        ''' <summary>
        ''' The collection of posts being rendered.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property PostCollection() As List(Of PostInfo)
            Get
                Return _PostCollection
            End Get
            Set(ByVal Value As List(Of PostInfo))
                _PostCollection = Value
            End Set
        End Property

        ''' <summary>
        ''' The page index being rendered (of the thread).
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property PostPage() As Integer
            Get
                Return _PostPage
            End Get
            Set(ByVal Value As Integer)
                _PostPage = Value
            End Set
        End Property

        ''' <summary>
        ''' If the user is tracking the containing forum (email notifications). 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property TrackedForum() As Boolean
            Get
                Return _TrackedForum
            End Get
            Set(ByVal Value As Boolean)
                _TrackedForum = Value
            End Set
        End Property

        ''' <summary>
        ''' if the user is tracking the thread (email notifications). 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Property TrackedThread() As Boolean
            Get
                Return _TrackedThread
            End Get
            Set(ByVal Value As Boolean)
                _TrackedThread = Value
            End Set
        End Property

#End Region

#Region "Event Handlers"

        ''' <summary>
        ''' Updates the thread status
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' </remarks>
        Protected Sub ddlThreadStatus_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim ThreadStatus As Integer = ddlThreadStatus.SelectedIndex
            Dim ctlThread As New ThreadController

            Dim ModeratorID As Integer = -1
            If CurrentForumUser.UserID <> objThread.StartedByUserID Then
                ModeratorID = CurrentForumUser.UserID
            End If

            ctlThread.ChangeThreadStatus(ThreadID, CurrentForumUser.UserID, ThreadStatus, 0, ModeratorID, PortalID)

            Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID)
        End Sub

        ''' <summary>
        ''' This Event turns the users thread tracking on/off.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' </remarks>
        Protected Sub chkEmail_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim ctlTracking As New TrackingController
            ctlTracking.TrackingThreadCreateDelete(ForumID, ThreadID, CurrentForumUser.UserID, chkEmail.Checked, ModuleID)
            'Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadId)
        End Sub

        ''' <summary>
        ''' Applies the user's thread rating.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>This needs to have redirect link generated from link utils.</remarks>
        Protected Sub trcRating_Rate(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim rate As Double = trcRating.Value

            If rate > 0 Then
                Dim ctlThread As New ThreadController
                ctlThread.ThreadRateAdd(ThreadID, CurrentForumUser.UserID, rate)
            End If

            Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID)
        End Sub

        ''' <summary>
        ''' Adds a user's vote to the data store for a specific poll.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdVote_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
            ' get the user's vote and put it in the data store
            Dim intAnswerID As Integer = CInt(rblstPoll.SelectedValue)
            ' update user voting, when page is redrawn it will handle checking if user voted
            Dim cntUserAnswer As New UserAnswerController
            Dim objUserAnswer As New UserAnswerInfo

            objUserAnswer.UserID = CurrentForumUser.UserID
            objUserAnswer.PollID = objThread.PollID
            objUserAnswer.AnswerID = intAnswerID

            cntUserAnswer.AddUserAnswer(objUserAnswer)
            ' update user answer cache - 
        End Sub

        ''' <summary>
        ''' Adds or remove the current thread to the users bookmark list 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdBookmark_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs)
            Dim BookmarkCtl As New BookmarkController
            Select Case cmdBookmark.AlternateText
                Case ForumControl.LocalizedText("RemoveBookmark")
                    BookmarkCtl.BookmarkCreateDelete(ThreadID, CurrentForumUser.UserID, False, ModuleID)
                    'Change ImageButton to support AJAX
                    cmdBookmark.AlternateText = ForumControl.LocalizedText("AddBookmark")
                    cmdBookmark.ToolTip = ForumControl.LocalizedText("AddBookmark")
                    cmdBookmark.ImageUrl = objConfig.GetThemeImageURL("forum_bookmark.") & objConfig.ImageExtension
                Case ForumControl.LocalizedText("AddBookmark")
                    BookmarkCtl.BookmarkCreateDelete(ThreadID, CurrentForumUser.UserID, True, ModuleID)
                    'Change ImageButton to support AJAX
                    cmdBookmark.AlternateText = ForumControl.LocalizedText("RemoveBookmark")
                    cmdBookmark.ToolTip = ForumControl.LocalizedText("RemoveBookmark")
                    cmdBookmark.ImageUrl = objConfig.GetThemeImageURL("forum_nobookmark.") & objConfig.ImageExtension
            End Select
        End Sub

        ''' <summary>
        ''' This takes moderators/forum admin to moderator screen with the thread loaded to view subscribers. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdThreadSubscribers_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim url As String
            url = Utilities.Links.ThreadEmailSubscribers(TabID, ModuleID, ForumID, ThreadID)
            MyBase.BasePage.Response.Redirect(url, False)
        End Sub

        ''' <summary>
        ''' This Event sets the users view preference ascending/descending and saves to 
        ''' the db. (Descending by default)
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Anonymous users can see both views but it doesn't save to db when changed.
        ''' </remarks>
        Protected Sub ddlViewDescending_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            ForumControl.Descending = CType(ddlViewDescending.SelectedIndex, Boolean)

            Dim ctlPost As New PostController
            PostCollection = ctlPost.PostGetAll(ThreadID, PostPage, CurrentForumUser.PostsPerPage, ForumControl.Descending, PortalID)

            If CurrentForumUser.UserID > 0 Then
                Dim ctlForumUser As New ForumUserController
                ctlForumUser.UpdateUsersView(CurrentForumUser.UserID, PortalID, ForumControl.Descending)
            End If
        End Sub

        ''' <summary>
        ''' This directs the user to the search results of this particular forum. It searches this forum and the subject, body of the post. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdForumSearch_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs)
            If txtForumSearch.Text.Trim <> String.Empty Then
                Dim url As String
                url = Utilities.Links.ContainerSingleForumSearchLink(TabID, ForumID, txtForumSearch.Text)
                MyBase.BasePage.Response.Redirect(url, False)
            End If
        End Sub

        ''' <summary>
        ''' Submits a quickly reply to the posting API (which is related to an existing thread). 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdSubmit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
            If Len(txtQuickReply.Text) > 0 Then
                Dim RemoteAddress As String = "0.0.0.0"
                Dim strSubject As String = Utilities.ForumUtils.SetReplySubject(objThread.Subject)

                If Not HttpContext.Current.Request.UserHostAddress Is Nothing Then
                    RemoteAddress = HttpContext.Current.Request.UserHostAddress
                End If

                Dim cntPostConnect As New PostConnector
                Dim PostMessage As PostMessage

                Dim textReply As String = txtQuickReply.Text

                'textReply = textReply.Replace(vbCrLf, "<br />")
                'textReply = textReply.Replace(Environment.NewLine, "<br />")
                ' Hammond
                textReply = textReply.Replace(ControlChars.Lf, "<br />")
                'textReply = textReply.Replace("\n", "<br />")

                PostMessage = cntPostConnect.SubmitInternalPost(TabID, ModuleID, PortalID, CurrentForumUser.UserID, strSubject, textReply, ForumID, objThread.ThreadID, -1, objThread.IsPinned, False, False, objThread.ThreadStatus, "", RemoteAddress, objThread.PollID, False, objThread.ThreadID, objThread.Terms)

                Select Case PostMessage
                    Case PostMessage.PostApproved
                        '	Dim ReturnURL As String = NavigateURL()

                        '	If objModSecurity.IsModerator Then
                        '		If Not ViewState("UrlReferrer") Is Nothing Then
                        '			ReturnURL = (CType(ViewState("UrlReferrer"), String))
                        '		Else
                        '			ReturnURL = Utilities.Links.ContainerViewForumLink(TabID, objForum.ForumID, False)
                        '		End If
                        '	Else
                        '		ReturnURL = Utilities.Links.ContainerViewForumLink(TabID, ForumId, False)
                        '	End If

                        '	Response.Redirect(ReturnURL, False)
                    Case PostMessage.PostModerated
                        'tblNewPost.Visible = False
                        'tblOldPost.Visible = False
                        'tblPreview.Visible = False
                        'cmdCancel.Visible = False
                        'cmdBackToEdit.Visible = False
                        'cmdSubmit.Visible = False
                        'cmdPreview.Visible = False
                        'cmdBackToForum.Visible = True
                        'rowModerate.Visible = True
                        'tblPoll.Visible = False
                    Case Else
                        'lblInfo.Visible = True
                        'lblInfo.Text = Localization.GetString(PostMessage.ToString() + ".Text", LocalResourceFile)
                End Select
                txtQuickReply.Text = ""
                'Forum.ThreadInfo.ResetThreadInfo(ThreadId)

                Dim ctlPost As New PostController
                PostCollection = ctlPost.PostGetAll(ThreadID, PostPage, CurrentForumUser.PostsPerPage, ForumControl.Descending, PortalID)
                ' we need to redirect the user here to make sure the page is redrawn.
            Else
                ' there is no quick reply message entered, yet they clicked submit. Show end user. 
            End If
        End Sub

        ''' <summary>
        ''' Sets a specific post as an answer, only available when thread status is set to 'unresolved'. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub cmdThreadAnswer_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.CommandEventArgs)
            Dim ctlThread As New ThreadController
            Dim answerPostID As Integer
            Dim Argument As String = String.Empty

            If e.CommandName = "MarkAnswer" Then
                Argument = CStr(e.CommandArgument)
                answerPostID = Int32.Parse(Argument)

                Dim ctlPost As New PostController
                Dim objPostInfo As PostInfo
                objPostInfo = ctlPost.GetPostInfo(answerPostID, PortalID)

                Dim ModeratorID As Integer = -1
                If objThread.StartedByUserID <> CurrentForumUser.UserID Then
                    ModeratorID = CurrentForumUser.UserID
                End If

                ctlThread.ChangeThreadStatus(ThreadID, objPostInfo.UserID, ThreadStatus.Answered, answerPostID, ModeratorID, PortalID)

                Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID)
            End If
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Instantiates this class, sets the page title and does a security check.
        ''' </summary>
        ''' <param name="forum"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal forum As DNNForum)
            MyBase.New(forum)

            Dim user As ForumUserInfo = CurrentForumUser

            If PostID > 0 Then
                Dim objPostCnt As New PostController
                Dim objPost As PostInfo = objPostCnt.GetPostInfo(PostID, PortalID)
                ThreadID = objPost.ThreadID

                ' we need to determine which page to return based on number of posts in this thread, the users posts per page count, and their asc/desc view, where this post is
                Dim cntThread As New ThreadController()
                objThread = cntThread.GetThread(ThreadID)

                ' we need to see if there is a content item for the thread, if not create one.
                If objThread.ContentItemId < 1 Then
                    Dim cntContent As New Content
                    objThread.ModuleID = ModuleID
                    objThread.TabID = TabID
                    objThread.SitemapInclude = objPost.ParentThread.ContainingForum.EnableSitemap

                    cntContent.CreateContentItem(objThread, TabID)

                    DotNetNuke.Modules.Forum.Components.Utilities.Caching.UpdateThreadCache(objThread.ThreadID)
                    objThread = cntThread.GetThread(ThreadID)
                End If

                Dim TotalPosts As Integer = objThread.Replies + 1
                Dim TotalPages As Integer = (CInt(TotalPosts / CurrentForumUser.PostsPerPage))
                Dim ThreadPageToShow As Integer = 1

                If user.ViewDescending Then
                    ThreadPageToShow = CInt(Math.Ceiling((objPost.PostsAfter + 1) / CurrentForumUser.PostsPerPage))
                Else
                    ThreadPageToShow = CInt(Math.Ceiling((objPost.PostsBefore + 1) / CurrentForumUser.PostsPerPage))
                End If
                PostPage = ThreadPageToShow
            Else
                If ThreadID > 0 Then
                    Dim cntThread As New ThreadController()
                    objThread = cntThread.GetThread(ThreadID)

                    ' we need to see if there is a content item for the thread, if not create one.
                    If objThread.ContentItemId < 1 Then
                        Dim cntContent As New Content
                        objThread.ModuleID = ModuleID
                        objThread.TabID = TabID
                        objThread.SitemapInclude = objThread.ContainingForum.EnableSitemap

                        cntContent.CreateContentItem(objThread, TabID)

                        DotNetNuke.Modules.Forum.Components.Utilities.Caching.UpdateThreadCache(objThread.ThreadID)
                        objThread = cntThread.GetThread(ThreadID)
                    End If

                    ' We need to make sure the user's thread pagesize can handle this 
                    '(problem is, a link can be posted by one user w/ page size of 5 pointing to page 2, if logged in user has pagesize set to 15, there is no page 2)
                    If Not HttpContext.Current.Request.QueryString("threadpage") Is Nothing Then
                        Dim urlThreadPage As Integer = Int32.Parse(HttpContext.Current.Request.QueryString("threadpage"))
                        Dim TotalPosts As Integer = objThread.Replies + 1

                        Dim TotalPages As Integer = CInt(Math.Ceiling(TotalPosts / CurrentForumUser.PostsPerPage))
                        Dim ThreadPageToShow As Integer

                        ' We need to check if it is possible for a pagesize in the URL for the user browsing (happens when coming from posted link by other user)
                        If TotalPages >= urlThreadPage Then
                            ThreadPageToShow = urlThreadPage
                        Else
                            ' We know for this user, total pages > user posts per page. Because of this, we know its not user using page change so show thread as normal
                            ThreadPageToShow = 0
                        End If
                        PostPage = ThreadPageToShow
                    End If
                End If
            End If

            ' If the thread info is nothing, it is probably a deleted thread
            If objThread Is Nothing Then
                ' we should consider setting type of redirect here?

                MyBase.BasePage.Response.Redirect(Utilities.Links.NoContentLink(TabID, ModuleID), True)
            End If

            ' Make sure the forum is active 
            If Not objThread.ContainingForum.IsActive Then
                ' we should consider setting type of redirect here?

                MyBase.BasePage.Response.Redirect(Utilities.Links.NoContentLink(TabID, ModuleID), True)
            End If

            ' User might access this page by typing url so better check permission on parent forum
            If Not (objThread.ContainingForum.PublicView) Then
                If Not objSecurity.IsAllowedToViewPrivateForum Then
                    ' we should consider setting type of redirect here?

                    MyBase.BasePage.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
                End If
            End If

            If objConfig.OverrideTitle Then
                Dim Title As String
                Dim Subject As String

                If objThread.Subject.Length > Constants.SEO_TITLE_LIMIT Then
                    Subject = objThread.Subject.Substring(0, Constants.SEO_TITLE_LIMIT)
                Else
                    Subject = objThread.Subject
                End If

                If Not Subject.Length > Constants.SEO_TITLE_LIMIT Then
                    Title = Subject

                    Subject += " - " & objThread.ContainingForum.Name
                    If Not Subject.Length > Constants.SEO_TITLE_LIMIT Then
                        Title = Subject

                        Subject += " - " & Me.BaseControl.PortalName
                        If Not Subject.Length > Constants.SEO_TITLE_LIMIT Then
                            Title = Subject
                        End If
                    End If
                Else
                    Title = Subject
                End If

                MyBase.BasePage.Title = Title
            End If

            If objConfig.OverrideDescription Then
                Dim Description As String

                If objThread.Subject.Length < Constants.SEO_DESCRIPTION_LIMIT Then
                    Description = objThread.Subject
                Else
                    Description = objThread.Subject.Substring(0, Constants.SEO_DESCRIPTION_LIMIT)
                End If

                MyBase.BasePage.Description = Description
            End If

            If objConfig.OverrideKeyWords Then
                Dim KeyWords As String = ""
                Dim keyCount As Integer = 0

                If objThread.ContainingForum.ParentID = 0 Then
                    KeyWords = objThread.ContainingForum.Name
                    keyCount = 1
                Else
                    KeyWords = objThread.ContainingForum.ParentForum.Name + "," + objThread.ContainingForum.Name
                    keyCount = 2
                End If

                If objConfig.EnableTagging Then
                    For Each Term As Entities.Content.Taxonomy.Term In objThread.Terms
                        If keyCount < Constants.SEO_KEYWORDS_LIMIT Then
                            KeyWords += "," + Term.Name
                            keyCount += 1
                        Else
                            Exit For
                        End If
                    Next

                    ' If we haven't hit the keyword limit, let's add portal name to the list.
                    If keyCount < Constants.SEO_KEYWORDS_LIMIT Then
                        KeyWords += "," + Me.BaseControl.PortalName
                    End If
                End If

                MyBase.BasePage.KeyWords = KeyWords
            End If

            If PostPage > 0 Then
                PostPage = PostPage - 1
            Else
                PostPage = 0
            End If
        End Sub

        ''' <summary>
        ''' This is the first class that runs as part of New().  This could be invoked in Render as well but is not
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        Public Overrides Sub CreateChildControls()
            Controls.Clear()

            'CP: NOTE: Telerik conversion
            'Me.trcRating = New Telerik.Web.UI.RadRating
            Me.trcRating = New Telerik.Web.UI.RadRating
            With trcRating
                .Skin = "Office2007"
                .SelectionMode = Telerik.Web.UI.RatingSelectionMode.Continuous
                .IsDirectionReversed = False
                .Orientation = Orientation.Horizontal
                .Precision = Telerik.Web.UI.RatingPrecision.Half
                .ItemCount = objConfig.RatingScale
                AddHandler trcRating.Rate, AddressOf trcRating_Rate
                .AutoPostBack = True
            End With

            ' display tracking option only if user authenticated
            If CurrentForumUser.UserID > 0 Then
                ' Thread Status Dropdownlist
                Me.ddlThreadStatus = New DotNetNuke.Web.UI.WebControls.DnnComboBox
                With ddlThreadStatus
                    .ID = "lstThreadStatus"
                    .Width = Unit.Parse("150")
                    .AutoPostBack = True
                    .ClearSelection()
                End With

                ' Email notification checkbox
                chkEmail = New CheckBox
                With chkEmail
                    .CssClass = "Forum_NormalTextBox"
                    .ID = "chkEmail"
                    .Text = ForumControl.LocalizedText("MailWhenReply").Replace("[ThreadSubject]", "<b>" & objThread.Subject & "</b>")
                    .TextAlign = TextAlign.Left
                    .AutoPostBack = True
                    .Checked = False
                End With
            End If

            ' Forum view (newest to oldest/oldest to newest) dropdownlist
            ddlViewDescending = New DotNetNuke.Web.UI.WebControls.DnnComboBox
            With ddlViewDescending
                .ID = "lstViewDescending"
                .Width = Unit.Parse("150")
                .AutoPostBack = True
                'CP: NOTE: Telerik conversion
                .Items.Add(New Telerik.Web.UI.RadComboBoxItem(ForumControl.LocalizedText("OldestToNewest")))
                .Items.Add(New Telerik.Web.UI.RadComboBoxItem(ForumControl.LocalizedText("NewestToOldest")))
                '.Items.Add(New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(ForumControl.LocalizedText("OldestToNewest")))
                '.Items.Add(New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(ForumControl.LocalizedText("NewestToOldest")))
                .ClearSelection()
            End With

            txtForumSearch = New TextBox
            With txtForumSearch
                .CssClass = "Forum_NormalTextBox"
                .ID = "txtForumSearch"
                .Width = Unit.Parse("150")
            End With

            Me.cmdForumSearch = New ImageButton
            With cmdForumSearch
                .CssClass = "Forum_Profile"
                .ID = "cmdForumSearch"
                .AlternateText = ForumControl.LocalizedText("Search")
                .ToolTip = ForumControl.LocalizedText("Search")
                .ImageUrl = objConfig.GetThemeImageURL("s_lookup.") & objConfig.ImageExtension
            End With

            If objConfig.HideSearchButton = True Then
                txtForumSearch.Visible = False
                cmdForumSearch.Visible = False
            End If

            'Polls
            Me.rblstPoll = New RadioButtonList
            With rblstPoll
                .CssClass = "Forum_NormalTextBox"
                .ID = "rblstPoll"
            End With

            Me.cmdVote = New LinkButton
            With cmdVote
                .CssClass = "Forum_Profile"
                .ID = "cmdVote"
                .Text = ForumControl.LocalizedText("Vote")
            End With

            If CurrentForumUser.UserID > 0 Then
                Me.cmdBookmark = New ImageButton
                With cmdBookmark
                    .CssClass = "Forum_Profile"
                    .ID = "cmdBookmark"
                End With
                Dim BookmarkCtl As New BookmarkController
                If BookmarkCtl.BookmarkCheck(CurrentForumUser.UserID, ThreadID, ModuleID) = True Then
                    With cmdBookmark
                        .AlternateText = ForumControl.LocalizedText("RemoveBookmark")
                        .ToolTip = ForumControl.LocalizedText("RemoveBookmark")
                        .ImageUrl = objConfig.GetThemeImageURL("forum_nobookmark.") & objConfig.ImageExtension
                    End With
                Else
                    With cmdBookmark
                        .AlternateText = ForumControl.LocalizedText("AddBookmark")
                        .ToolTip = ForumControl.LocalizedText("AddBookmark")
                        .ImageUrl = objConfig.GetThemeImageURL("forum_bookmark.") & objConfig.ImageExtension
                    End With
                End If
            End If

            If Not CurrentForumUser.UserID > 0 Then
                ddlViewDescending.Visible = False
            End If

            ' Tags
            Me.tagsControl = New DotNetNuke.Web.UI.WebControls.Tags
            With tagsControl
                .ID = "tagsControl"
                ' if we come up w/ our own tagging window, this needs to be changed to false.
                .AllowTagging = HttpContext.Current.Request.IsAuthenticated
                .NavigateUrlFormatString = DotNetNuke.Common.Globals.NavigateURL(PortalUtilityClass.SearchTagSearchTabID(PortalID), "", "Tag={0}")
                .RepeatDirection = "Horizontal"
                .Separator = ","
                ' TODO: We may want to show this in future, for now we are leaving categories out of the mix.
                .ShowCategories = False
                .ShowTags = True
                .AddImageUrl = "~/images/add.gif"
                .CancelImageUrl = "~/images/lt.gif"
                .SaveImageUrl = "~/images/save.gif"
                .CssClass = "SkinObject"
            End With

            ' Quick Reply
            Me.txtQuickReply = New TextBox
            With txtQuickReply
                .CssClass = "Forum_NormalTextBox"
                .ID = "txtQuickReply"
                .Width = Unit.Percentage(99)
                .Height = 150
                .TextMode = TextBoxMode.MultiLine
                '.Text
            End With

            Me.cmdSubmit = New LinkButton
            With cmdSubmit
                .CssClass = "Forum_Link"
                .ID = "cmdSubmit"
                .Text = ForumControl.LocalizedText("cmdSubmit")
                .OnClientClick = "if (!Page_ClientValidate()){ return false; } this.disabled = true; this.value = '';"
            End With

            Me.cmdThreadSubscribers = New LinkButton
            With cmdThreadSubscribers
                .CssClass = "Forum_Profile"
                .ID = "cmdThreadSubscribers"
                .Text = ForumControl.LocalizedText("cmdThreadSubscribers")
            End With

            BindControls()
            AddControlHandlers()
            AddControlsToTree()

            For Each post As PostInfo In PostCollection
                Me.cmdThreadAnswer = New System.Web.UI.WebControls.LinkButton
                With cmdThreadAnswer
                    .CssClass = "Forum_AnswerText"
                    .ID = "cmdThreadAnswer" + post.PostID.ToString()
                    .Text = ForumControl.LocalizedText("MarkAnswer")
                    .CommandName = "MarkAnswer"
                    .CommandArgument = post.PostID.ToString()
                    AddHandler cmdThreadAnswer.Command, AddressOf cmdThreadAnswer_Click
                End With
                hsThreadAnswers.Add(post.PostID, cmdThreadAnswer)
                Controls.Add(cmdThreadAnswer)
            Next
        End Sub

        ''' <summary>
        ''' Does the actual calls for rendering the UI in logical order to build wr
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>
        ''' </remarks>
        Public Overrides Sub Render(ByVal wr As HtmlTextWriter)
            RenderTableBegin(wr, "tblForumContainer", "Forum_Container", "", "100%", "0", "0", "", "", "0")
            RenderNavBar(wr, objConfig, ForumControl)
            RenderSearchBar(wr)
            RenderTopBreadcrumb(wr)
            RenderTopThreadButtons(wr)
            RenderThread(wr)
            RenderBottomThreadButtons(wr)
            RenderBottomBreadCrumb(wr)
            RenderTags(wr)
            RenderQuickReply(wr)
            RenderThreadOptions(wr)
            RenderTableEnd(wr)

            'increment the thread view count
            Dim ctlThread As New ThreadController
            ctlThread.IncrementThreadViewCount(ThreadID)

            'update the UserThread record
            If HttpContext.Current.Request.IsAuthenticated Then
                Dim userThreadController As New UserThreadsController
                Dim userThread As New UserThreadsInfo
                userThread = userThreadController.GetThreadReadsByUser(CurrentForumUser.UserID, ThreadID)

                If Not userThread Is Nothing Then
                    userThread.LastVisitDate = Now
                    ' Add error handling Just in case because of constraints and data integrity - This is highly unlikely to occur so do it here instead of the database(performance reasons)
                    Try
                        userThreadController.Update(userThread)
                        UserThreadsController.ResetUserThreadReadCache(userThread.UserID, ThreadID)
                    Catch exc As Exception
                        LogException(exc)
                    End Try
                Else
                    userThread = New UserThreadsInfo
                    With userThread
                        .UserID = CurrentForumUser.UserID
                        .ThreadID = ThreadID
                        .LastVisitDate = Now
                    End With
                    userThreadController.Add(userThread)
                    UserThreadsController.ResetUserThreadReadCache(userThread.UserID, userThread.ThreadID)
                End If
                ' Not sure we should keep this, we are basically updating a thread cache item if a new view was added. Is this really necessary?
                Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID)
            End If
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Sets handlers for certain server controls
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        Private Sub AddControlHandlers()
            Try
                If objConfig.EnableThreadStatus And CurrentForumUser.UserID > 0 Then
                    AddHandler ddlThreadStatus.SelectedIndexChanged, AddressOf ddlThreadStatus_SelectedIndexChanged
                End If

                If objConfig.MailNotification And CurrentForumUser.UserID > 0 Then
                    AddHandler chkEmail.CheckedChanged, AddressOf chkEmail_CheckedChanged
                End If

                If objConfig.EnableRatings Then
                    AddHandler trcRating.Rate, AddressOf trcRating_Rate
                End If

                If CurrentForumUser.UserID > 0 Then
                    AddHandler cmdBookmark.Click, AddressOf cmdBookmark_Click
                    AddHandler cmdThreadSubscribers.Click, AddressOf cmdThreadSubscribers_Click
                    ' Move out to support anon posting (if we allow quick reply via anonymous posting)
                    AddHandler cmdSubmit.Click, AddressOf cmdSubmit_Click
                    ' Move otu to support anon poll voting (after posting is supported)
                    AddHandler cmdVote.Click, AddressOf cmdVote_Click
                End If

                AddHandler ddlViewDescending.SelectedIndexChanged, AddressOf ddlViewDescending_SelectedIndexChanged
                If objConfig.HideSearchButton = False Then
                    AddHandler cmdForumSearch.Click, AddressOf cmdForumSearch_Click
                End If
            Catch exc As Exception
                LogException(exc)
            End Try
        End Sub

        ''' <summary>
        ''' Adds the controls to the control tree
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        Private Sub AddControlsToTree()
            Try
                If objConfig.EnableThreadStatus And CurrentForumUser.UserID > 0 Then
                    Controls.Add(ddlThreadStatus)
                End If

                If objConfig.MailNotification And CurrentForumUser.UserID > 0 Then
                    Controls.Add(chkEmail)
                End If

                If objConfig.EnableRatings Then
                    Controls.Add(trcRating)
                End If

                Controls.Add(rblstPoll)

                If CurrentForumUser.UserID > 0 Then
                    Controls.Add(cmdBookmark)
                    Controls.Add(cmdThreadSubscribers)

                    ' move for anon posting (if we allow quick reply via anonymous posting)
                    Controls.Add(txtQuickReply)
                    Controls.Add(cmdSubmit)
                    Controls.Add(cmdVote)
                End If

                Controls.Add(tagsControl)
                Controls.Add(ddlViewDescending)
                If objConfig.HideSearchButton = False Then
                    Controls.Add(txtForumSearch)
                    Controls.Add(cmdForumSearch)
                End If
            Catch exc As Exception
                LogException(exc)
            End Try
        End Sub

        ''' <summary>
        ''' Binds data to the available controls to the end user
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        Private Sub BindControls()
            Try
                Dim ctlPost As New PostController

                If objConfig.EnableRatings Then
                    BindRating()
                End If

                ' All enclosed items are user specific, so we must have a userID
                If CurrentForumUser.UserID > 0 Then
                    If objConfig.EnableThreadStatus And objThread.ContainingForum.EnableForumsThreadStatus Then
                        ddlThreadStatus.Visible = True
                        ddlThreadStatus.Items.Clear()

                        'CP: NOTE: Telerik conversion
                        ddlThreadStatus.Items.Insert(0, New Telerik.Web.UI.RadComboBoxItem(Localization.GetString("NoneSpecified", objConfig.SharedResourceFile), "0"))
                        ddlThreadStatus.Items.Insert(1, New Telerik.Web.UI.RadComboBoxItem(Localization.GetString("Unanswered", objConfig.SharedResourceFile), "1"))
                        ddlThreadStatus.Items.Insert(2, New Telerik.Web.UI.RadComboBoxItem(Localization.GetString("Answered", objConfig.SharedResourceFile), "2"))
                        ddlThreadStatus.Items.Insert(3, New Telerik.Web.UI.RadComboBoxItem(Localization.GetString("Informative", objConfig.SharedResourceFile), "3"))
                        'ddlThreadStatus.Items.Insert(0, New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(Localization.GetString("NoneSpecified", objConfig.SharedResourceFile), "0"))
                        'ddlThreadStatus.Items.Insert(1, New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(Localization.GetString("Unanswered", objConfig.SharedResourceFile), "1"))
                        'ddlThreadStatus.Items.Insert(2, New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(Localization.GetString("Answered", objConfig.SharedResourceFile), "2"))
                        'ddlThreadStatus.Items.Insert(3, New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(Localization.GetString("Informative", objConfig.SharedResourceFile), "3"))
                    Else
                        ddlThreadStatus.Visible = False
                    End If
                    'polling changes
                    If objThread.ThreadStatus = ThreadStatus.Poll Then
                        'CP: NOTE: Telerik conversion
                        Dim statusEntry As New Telerik.Web.UI.RadComboBoxItem(Localization.GetString("Poll", objConfig.SharedResourceFile), ThreadStatus.Poll.ToString())
                        'Dim statusEntry As New DotNetNuke.Wrapper.UI.WebControls.DnnComboBoxItem(Localization.GetString("Poll", objConfig.SharedResourceFile), ThreadStatus.Poll.ToString())
                        ddlThreadStatus.Items.Add(statusEntry)
                    End If

                    ddlThreadStatus.SelectedIndex = CType(objThread.ThreadStatus, Integer)

                    ' display tracking option only if user is authenticated and the forum module allows tracking
                    If objConfig.MailNotification Then
                        ' check to see if the user is tracking at the forum level
                        For Each objTrackForum As TrackingInfo In CurrentForumUser.TrackedForums(ModuleID)
                            If objTrackForum.ForumID = ForumID Then
                                TrackedForum = True
                                Exit For
                            End If
                        Next

                        If Not TrackedForum Then
                            Dim arrTrackThreads As List(Of TrackingInfo) = CurrentForumUser.TrackedThreads(ModuleID)
                            Dim objTrackThread As TrackingInfo

                            ' check to see if the user is tracking at the thread level
                            For Each objTrackThread In arrTrackThreads
                                If objTrackThread.ThreadID = ThreadID Then
                                    TrackedThread = True
                                    chkEmail.Checked = True
                                    Exit For
                                End If
                            Next
                        End If
                    End If

                    If (CurrentForumUser.ViewDescending) Then
                        ForumControl.Descending = True
                        ddlViewDescending.Items.FindItemByText(ForumControl.LocalizedText("NewestToOldest")).Selected = True
                    Else
                        ForumControl.Descending = False
                        ddlViewDescending.Items.FindItemByText(ForumControl.LocalizedText("OldestToNewest")).Selected = True
                    End If

                    ' Handle Polls
                    If objThread.PollID > 0 Then
                        Dim cntAnswer As New AnswerController
                        Dim arrAnswers As List(Of AnswerInfo)

                        arrAnswers = cntAnswer.GetPollAnswers(objThread.PollID)
                        If arrAnswers.Count > 0 Then
                            rblstPoll.DataTextField = "Answer"
                            rblstPoll.DataValueField = "AnswerID"
                            rblstPoll.DataSource = arrAnswers
                            rblstPoll.DataBind()

                            rblstPoll.SelectedIndex = 0
                        End If
                    End If
                Else
                    ForumControl.Descending = CType(ddlViewDescending.SelectedIndex, Boolean)
                    'CP - COMEBACK: Add way to display rating but don't allow voting (for anonymous users)
                    trcRating.Enabled = False
                End If

                tagsControl.ContentItem = DotNetNuke.Entities.Content.Common.Util.GetContentController().GetContentItem(objThread.ContentItemId)

                PostCollection = ctlPost.PostGetAll(ThreadID, PostPage, CurrentForumUser.PostsPerPage, ForumControl.Descending, PortalID)
            Catch exc As Exception
                LogException(exc)
            End Try
        End Sub

        ''' <summary>
        ''' Binds the current users rating to the rating control, also enables/disables the control.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub BindRating()
            trcRating.Value = CDec(objThread.Rating)
            trcRating.ToolTip = objThread.RatingText

            If Not CurrentForumUser.UserID > 0 Then
                trcRating.Enabled = False
            End If
        End Sub

        ''' <summary>
        ''' Renders the Rating selector, current rating image, search textbox and button
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderSearchBar(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<tr>

            ' left cap
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")

            RenderCellBegin(wr, "", "", "100%", "", "", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) '<tr>

            RenderCellBegin(wr, "", "", "100%", "left", "", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "", "2", "0", "", "", "0")  ' <table>
            RenderRowBegin(wr) '<tr>

            '[skeel] Display bookmark image button here
            If CurrentForumUser.UserID > 0 Then
                RenderCellBegin(wr, "", "", "", "left", "", "", "") ' <td> 
                cmdBookmark.RenderControl(wr)
                RenderCellEnd(wr) ' </td>
            End If

            ' Display rating only if user is authenticated
            If PostCollection.Count > 0 Then
                'check to see if new setting, enable ratings is enabled
                If objConfig.EnableRatings And objThread.ContainingForum.EnableForumsRating Then
                    RenderCellBegin(wr, "", "", "", "left", "", "", "") ' <td> 
                    'CP - Sub in ajax image rating solution here for ddl
                    trcRating.RenderControl(wr)

                    ' See if user has set status, if so we need to bind it
                    RenderCellEnd(wr) ' </td>

                    RenderCellBegin(wr, "", "", "", "left", "", "", "")  ' <td> '
                    RenderCellEnd(wr) ' </td>
                End If
            Else
                RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td> 
                wr.Write("&nbsp;")
                RenderCellEnd(wr) ' </td>
            End If

            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>
            If objConfig.HideSearchButton = False Then
                RenderCellBegin(wr, "", "", "100%", "right", "middle", "", "")
                RenderTableBegin(wr, 0, 0, "InnerTable") '<table>
                RenderRowBegin(wr) ' <tr>
                RenderCellBegin(wr, "", "", "", "", "middle", "", "") ' <td>

                txtForumSearch.RenderControl(wr)
                RenderCellEnd(wr) ' </td>

                RenderCellBegin(wr, "", "", "", "", "middle", "", "") ' <td>
                cmdForumSearch.RenderControl(wr)
                RenderCellEnd(wr) ' </td>
                RenderRowEnd(wr) ' </tr>
                RenderTableEnd(wr) ' </table>

                RenderCellEnd(wr) ' </td>

            End If
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderRowEnd(wr) ' </tr>
        End Sub

        ''' <summary>
        ''' Renders the row w/ the navigation breadcrumb
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderTopBreadcrumb(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "") ' <td></td>
            RenderCellBegin(wr, "", "", "100%", "left", "bottom", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "100%", "", "", "2", "") ' <td> 

            Dim tempForumID As Integer
            If Not HttpContext.Current.Request.QueryString("forumid") Is Nothing Then
                tempForumID = Int32.Parse(HttpContext.Current.Request.QueryString("forumid"))
            End If
            Dim ChildGroupView As Boolean = False
            If CType(ForumControl.TabModuleSettings("groupid"), String) <> String.Empty Then
                ChildGroupView = True
            End If
            wr.Write(Utilities.ForumUtils.BreadCrumbs(TabID, ModuleID, ForumScope.Posts, objThread, objConfig, ChildGroupView))
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </Tr>
            RenderRowBegin(wr) '<tr>

            RenderCellBegin(wr, "", "", "100%", "", "", "2", "") ' <td> 
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </Tr>
            RenderRowBegin(wr) '<tr>

            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderCellBegin(wr, "", "", "100%", "", "", "", "") ' <td> 
            RenderCellEnd(wr) ' </td>

            RenderRowEnd(wr) ' </Tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </Td>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderRowEnd(wr) ' </Tr>
        End Sub

        ''' <summary>
        ''' Renders the area directly above the post including: New Thread, prev/next
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderTopThreadButtons(ByVal wr As HtmlTextWriter)
            Dim fSubject As String
            Dim url As String

            If PostCollection.Count > 0 Then
                Dim firstPost As PostInfo = CType(PostCollection(0), PostInfo)
                fSubject = String.Format("&nbsp;{0}", firstPost.Subject)
                ' filter bad words if required in forum settings
                If ForumControl.objConfig.FilterSubject Then
                    fSubject = Utilities.ForumUtils.FormatProhibitedWord(fSubject, firstPost.CreatedDate, PortalID)
                End If
            Else
                fSubject = ForumControl.LocalizedText("NoPost")
            End If

            RenderRowBegin(wr) '<tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")

            RenderCellBegin(wr, "", "", "100%", "left", "", "", "") '<td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "left", "", "0") ' <table>
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "70%", "left", "middle", "", "")  '<td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>

            RenderCellBegin(wr, "", "", "", "", "middle", "", "")   '<td>           

            ' new thread button
            'Remove LoggedOnUserID limitation if wishing to implement Anonymous Posting
            If (CurrentForumUser.UserID > 0) And (Not ForumID = -1) Then
                If Not objThread.ContainingForum.PublicPosting Then
                    If objSecurity.IsAllowedToStartRestrictedThread Then
                        RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                        RenderRowBegin(wr) '<tr>
                        url = Utilities.Links.NewThreadLink(TabID, ForumID, ModuleID)
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 

                        If CurrentForumUser.IsBanned Then
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link", False)
                        Else
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link")
                        End If

                        RenderCellEnd(wr) ' </Td>

                        If CurrentForumUser.IsBanned Or (Not objSecurity.IsAllowedToPostRestrictedReply) Or (objThread.IsClosed) Then
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                            RenderCellEnd(wr) ' </Td>
                        Else
                            url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        '[skeel] moved delete thread here
                        If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then

                            url = Utilities.Links.ThreadDeleteLink(TabID, ModuleID, ForumID, ThreadID, False)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("DeleteThread"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        RenderRowEnd(wr) ' </tr>
                        RenderTableEnd(wr) ' </table>
                    ElseIf objSecurity.IsAllowedToPostRestrictedReply Then
                        RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                        RenderRowBegin(wr) '<tr>

                        If CurrentForumUser.IsBanned Or objThread.IsClosed Then
                            url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)

                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                            RenderCellEnd(wr) ' </Td>
                        Else
                            url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        RenderRowEnd(wr) ' </tr>
                        RenderTableEnd(wr) ' </table>
                    Else
                        ' user cannot start thread or make a reply
                        wr.Write("&nbsp;")
                    End If
                Else
                    ' no posting restrictions
                    RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                    RenderRowBegin(wr) '<tr>
                    url = Utilities.Links.NewThreadLink(TabID, ForumID, ModuleID)
                    RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 

                    If CurrentForumUser.IsBanned Then
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link", False)
                    Else
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link")
                    End If

                    RenderCellEnd(wr) ' </Td>

                    If CurrentForumUser.IsBanned Or objThread.IsClosed Then
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                        RenderCellEnd(wr) ' </Td>
                    Else
                        url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                        RenderCellEnd(wr) ' </Td>
                    End If

                    '[skeel] moved delete thread here
                    If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then
                        url = Utilities.Links.ThreadDeleteLink(TabID, ModuleID, ForumID, ThreadID, False)
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("DeleteThread"), "Forum_Link")
                        RenderCellEnd(wr) ' </Td>
                    End If

                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                End If
            End If

            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>

            ' Thread navigation
            RenderCellBegin(wr, "", "", "30%", "right", "", "", "")  '<td> 
            RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>
            Dim PreviousEnabled As Boolean = False
            Dim EnabledText As String = "Disabled"

            If Not (objThread.PreviousThreadID = 0) Then
                If Not (objThread.IsPinned) Then
                    PreviousEnabled = True
                    EnabledText = "Previous"
                End If
            End If

            If PreviousEnabled Then
                RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "", "", "")  ' <td> ' 
            Else
                RenderCellBegin(wr, "Forum_NavBarButtonDisabled", "", "", "", "", "", "")   ' <td> ' 
            End If
            RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>

            url = Utilities.Links.ContainerViewThreadLink(TabID, ForumID, objThread.PreviousThreadID)

            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> ' 
            If PreviousEnabled Then
                RenderLinkButton(wr, url, ForumControl.LocalizedText("Previous"), "Forum_Link")
            Else
                RenderDivBegin(wr, "", "Forum_NormalBold")
                wr.Write(ForumControl.LocalizedText("Previous"))
                RenderDivEnd(wr)
            End If
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>    

            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> 
            wr.Write("&nbsp;")
            RenderCellEnd(wr) ' </td>

            Dim NextEnabled As Boolean = False
            Dim NextText As String = "Disabled"
            If Not (objThread.NextThreadID = 0) Then
                If Not (objThread.IsPinned = True) Then
                    NextEnabled = True
                    NextText = "Next"
                End If
            End If

            If NextEnabled Then
                RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "", "", "")  ' <td> '
            Else
                RenderCellBegin(wr, "Forum_NavBarButtonDisabled", "", "", "", "", "", "")   ' <td> '
            End If

            RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> ' 

            If NextEnabled Then
                url = Utilities.Links.ContainerViewThreadLink(TabID, ForumID, objThread.NextThreadID)
                RenderLinkButton(wr, url, ForumControl.LocalizedText("Next"), "Forum_Link")
            Else
                RenderDivBegin(wr, "", "Forum_NormalBold")
                wr.Write(ForumControl.LocalizedText("Next"))
                RenderDivEnd(wr)
            End If
            RenderCellEnd(wr) ' </td>   
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>

            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>

            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table> 
            RenderCellEnd(wr) ' </td>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderRowEnd(wr) ' </tr>       
        End Sub

        ''' <summary>
        ''' This area is used to render all individual posts and the footer  as well as any poll related UI (by calling other methods)
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderThread(ByVal wr As HtmlTextWriter)
            'CP - Spacer Row between final post footer and bottom panel
            RenderRowBegin(wr) '<tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderCellBegin(wr, "", "", "100%", "", "", "", "") '<td>
            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderRowEnd(wr) ' </tr>
            'End spacer row

            ' Handle polls
            If objThread.ContainingForum.AllowPolls And objThread.PollID > 0 And CurrentForumUser.UserID > 0 Then
                RenderPoll(wr)
            End If

            ' Loop round rows in selected thread (These are rows w/ user avatar/alias, post body)
            RenderPosts(wr)
            RenderFooter(wr)
        End Sub

        ''' <summary>
        ''' Renders a poll or the results (possibly thank you message if show results are off) if one is attached to a thread.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderPoll(ByVal wr As HtmlTextWriter)
            'new row
            RenderRowBegin(wr) '<tr>                
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")

            ' middle master column (this will hold a table, the poll post will display here, then render a spacer row to seperate it from the other posts 
            RenderCellBegin(wr, "", "", "", "left", "middle", "", "")
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table> 
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "100%", "", "", "", "") ' <td>

            ' table to hold poll header
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr, "") ' <tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapLeft", "") ' <td><img /></td>
            RenderCellBegin(wr, "Forum_Header", "", "", "", "", "", "")    '<td>

            Dim cntPoll As New PollController
            Dim objPoll As New PollInfo
            objPoll = cntPoll.GetPoll(objThread.PollID)

            RenderDivBegin(wr, "", "Forum_HeaderText") ' <span>
            wr.Write("&nbsp;" & ForumControl.LocalizedText("Poll") & ": " & objPoll.Question)
            RenderDivEnd(wr) ' </span>
            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapRight", "") ' <td><img /></td>
            RenderRowEnd(wr) ' </tr>

            RenderTableEnd(wr) ' </table>

            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>

            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "Forum_Avatar", "", "100%", "center", "middle", "", "")    '<td>

            Dim showPoll As Boolean = True
            If Not objPoll.PollClosed Then
                For Each objUserAnswer As UserAnswerInfo In objPoll.UserAnswers
                    If objUserAnswer.UserID = CurrentForumUser.UserID Then
                        showPoll = False
                        Exit For
                    End If
                Next
            End If

            If showPoll And (Not objPoll.PollClosed) Then
                ' if the user hasn't voted, show the the poll
                rblstPoll.RenderControl(wr)
                cmdVote.RenderControl(wr)
                ' Not implemented
                'If (LoggedOnUserID = ThreadInfo.StartedByUserID) Or (Security.IsForumModerator) Then
                '    'cmdViewResults.RenderControl(wr)
                'End If
            Else
                ' check to see if we are able to show user results
                If objPoll.ShowResults Or ((CurrentForumUser.UserID = objThread.StartedByUserID) Or (objSecurity.IsForumModerator)) Then
                    ' show results
                    RenderTableBegin(wr, "", "", "", "", "0", "0", "center", "middle", "")  ' <table> 

                    For Each objAnswer As AnswerInfo In objPoll.Answers
                        Dim cntAnswer As New AnswerController
                        objAnswer = cntAnswer.GetAnswer(objAnswer.AnswerID)

                        ' create a row representing results
                        RenderRowBegin(wr) ' <tr>
                        RenderCellBegin(wr, "", "", "", "left", "middle", "", "")    '<td>

                        ' show answer
                        RenderDivBegin(wr, "", "Forum_Normal") ' <span>
                        wr.Write(objAnswer.Answer & "&nbsp;")
                        RenderDivEnd(wr) ' </span>
                        RenderCellEnd(wr) ' </td>

                        ' handle calculation
                        Dim Percentage As Double
                        If objPoll.TotalVotes = 0 Then
                            Percentage = 0
                        Else
                            Percentage = (objAnswer.AnswerCount / objPoll.TotalVotes) * 100
                        End If

                        Dim strVoteCount As String
                        strVoteCount = objAnswer.AnswerCount.ToString()
                        strVoteCount = strVoteCount + " " + Localization.GetString("Votes", objConfig.SharedResourceFile)

                        ' show image
                        RenderCellBegin(wr, "", "", "", "left", "middle", "", "")    '<td>
                        If CType(Percentage, Integer) > 0 Then
                            RenderImage(wr, objConfig.GetThemeImageURL("poll_capleft.") & objConfig.ImageExtension, strVoteCount, "")
                            ' handle this biatch
                            Dim i As Integer = 0
                            For i = 0 To CType(Percentage, Integer)
                                RenderImage(wr, objConfig.GetThemeImageURL("poll_bar.") & objConfig.ImageExtension, strVoteCount, "")
                            Next
                            RenderImage(wr, objConfig.GetThemeImageURL("poll_capright.") & objConfig.ImageExtension, strVoteCount, "")
                        End If
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </td>

                        ' show percentage
                        RenderCellBegin(wr, "", "", "", "right", "middle", "", "")    '<td>
                        RenderDivBegin(wr, "", "Forum_Normal") ' <span>
                        wr.Write(FormatNumber(Percentage, 2).ToString() & " %")
                        RenderDivEnd(wr) ' </span>
                        RenderCellEnd(wr) ' </td>
                        RenderRowEnd(wr) ' </tr>
                    Next

                    RenderRowBegin(wr) ' <tr>
                    RenderCellBegin(wr, "", "", "100%", "center", "middle", "3", "")       '<td>
                    RenderDivBegin(wr, "", "Forum_NormalBold") ' <span>
                    wr.RenderBeginTag(HtmlTextWriterTag.B)
                    wr.Write(Localization.GetString("TotalVotes", objConfig.SharedResourceFile) & " " & objPoll.TotalVotes.ToString())
                    wr.RenderEndTag()
                    RenderDivEnd(wr) ' </span>
                    RenderCellEnd(wr) ' </td>
                    RenderRowEnd(wr) ' </tr>

                    '' View Details Row (Not Implemented)
                    'RenderRowBegin(wr) ' <tr>
                    'RenderCellBegin(wr, "", "", "100%", "center", "middle", "3", "")    '<td>
                    'RenderSpanBegin(wr, "", "Forum_Normal") ' <span>
                    'wr.Write("Total Votes: " & objPoll.TotalVotes.ToString())
                    'RenderSpanEnd(wr) ' </span>
                    'RenderCellEnd(wr) ' </td>
                    'RenderRowEnd(wr) ' </tr>

                    RenderTableEnd(wr) ' </table>
                Else
                    RenderDivBegin(wr, "", "Forum_Normal") ' <span>
                    wr.Write(objPoll.TakenMessage)
                    RenderDivEnd(wr) ' </span>
                End If
            End If
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>

            RenderRowBegin(wr) '<tr> 
            RenderCellBegin(wr, "Forum_SpacerRow", "", "", "", "", "", "")  ' <td>
            RenderImage(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "", "")
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")

            RenderRowEnd(wr) ' </tr>
        End Sub

        ''' <summary>
        ''' posts make up all rows in between (fourth row to third to last row, numerous rows)
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderPosts(ByVal wr As HtmlTextWriter)
            ' use a counter to determine odd/even for alternating colors (via css)
            Dim intPostCount As Integer = 1
            Dim totalPostCount As Integer = PostCollection.Count
            Dim currentCount As Integer = 1

            RenderRowBegin(wr) '<tr>                
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "", "") ' <td><img/></td>
            RenderCellBegin(wr, "", "", "100%", "", "top", "", "")  ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "center", "", "0")    ' <table> 

            For Each Post As PostInfo In PostCollection
                Dim postCountIsEven As Boolean = ThreadIsEven(intPostCount)
                Me.RenderPost(wr, Post, postCountIsEven)
                ' spacer row should be displayed in flat view only
                If Not currentCount = totalPostCount Then
                    RenderSpacerRow(wr)
                    currentCount += 1
                End If
                intPostCount += 1

                ' inject Advertisment into forum post list
                If (objConfig.AdsAfterFirstPost AndAlso intPostCount = 2) OrElse ((objConfig.AddAdverAfterPostNo <> 0) AndAlso ((intPostCount - 1) Mod objConfig.AddAdverAfterPostNo = 0)) Then
                    Me.RenderAdvertisementPost(wr)
                    RenderSpacerRow(wr)
                End If
            Next
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "", "") ' <td><img/></td>
            RenderRowEnd(wr) ' </tr>
        End Sub

        ''' <summary>
        ''' Renders the entire table structure of a single post
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="Post"></param>
        ''' <param name="PostCountIsEven"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderPost(ByVal wr As HtmlTextWriter, ByVal Post As PostInfo, ByVal PostCountIsEven As Boolean)
            Dim authorCellClass As String
            Dim bodyCellClass As String

            ' these classes to set bg color of cells
            If PostCountIsEven Then
                authorCellClass = "Forum_Avatar"
                bodyCellClass = "Forum_PostBody_Container"
            Else
                authorCellClass = "Forum_Avatar_Alt"
                bodyCellClass = "Forum_PostBody_Container_Alt"
            End If

            'Add per post header - better UI can add more info
            Dim strPostedDate As String = String.Empty
            Dim newpost As Boolean
            strPostedDate = Utilities.ForumUtils.GetCreatedDateInfo(Post.CreatedDate, objConfig, "").ToString

            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "100%", "left", "middle", "2", "")  '<td>
            '[skeel] Check if first new post and add bookmark used for navigation
            If HttpContext.Current.Request IsNot Nothing Then
                If HttpContext.Current.Request.IsAuthenticated Then
                    If Post.NewPost(CurrentForumUser.UserID) Then
                        RenderPostBookmark(wr, "unread")
                        newpost = True
                    End If
                End If
            End If

            '[skeel] add Bookmark to post
            'RenderPostBookmark(wr, "p" & CStr(Post.PostID))
            RenderPostBookmark(wr, CStr(Post.PostID))
            'Make table to hold per post header
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "center", "middle", "0")  ' <table> 
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "100%", "left", "middle", "", "")   '<td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "center", "middle", "0")  ' <table> 

            RenderRowBegin(wr) ' <tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapLeft", "") ' <td><img /></td>


            ' start post status image
            RenderCellBegin(wr, "Forum_Header_PostStatus", "", "", "left", "", "", "") '<td>
            ' display "new" image if this post is new since last time user visited the thread
            If HttpContext.Current.Request IsNot Nothing Then
                If HttpContext.Current.Request.IsAuthenticated Then
                    If Post.NewPost(CurrentForumUser.UserID) Then
                        RenderImage(wr, objConfig.GetThemeImageURL("s_new.") & objConfig.ImageExtension, ForumControl.LocalizedText("UnreadPost"), "")
                    Else
                        RenderImage(wr, objConfig.GetThemeImageURL("s_old.") & objConfig.ImageExtension, ForumControl.LocalizedText("ReadPost"), "")
                    End If
                Else
                    RenderImage(wr, objConfig.GetThemeImageURL("s_new.") & objConfig.ImageExtension, ForumControl.LocalizedText("UnreadPost"), "")
                End If
            Else
                RenderImage(wr, objConfig.GetThemeImageURL("s_new.") & objConfig.ImageExtension, ForumControl.LocalizedText("UnreadPost"), "")
            End If
            RenderCellEnd(wr) ' </td> 

            RenderCellBegin(wr, "Forum_Header", "", "", "left", "", "", "")      '<td>
            RenderDivBegin(wr, "", "Forum_HeaderText") ' <span>
            wr.Write(strPostedDate)
            RenderDivEnd(wr) ' </span>
            RenderCellEnd(wr) ' </td> 

            RenderCellBegin(wr, "Forum_Header_ThreadStatus", "", "", "right", "", "", "")       '<td>

            ' if the user is the original author or a moderator AND this is the original post
            If ((CurrentForumUser.UserID = Post.ParentThread.StartedByUserID) Or (objSecurity.IsForumModerator)) And Post.ParentPostID = 0 Then
                If Post.ParentThread.ThreadStatus = ThreadStatus.Poll Then
                    ddlThreadStatus.Enabled = False
                End If
                ddlThreadStatus.RenderControl(wr)
                'wr.Write("&nbsp;")
            Else
                ' this is either not the original post or the user is not the author or a moderator
                ' If the thread is answered AND this is the post accepted as the answer
                If Post.ParentThread.ThreadStatus = ThreadStatus.Answered And (Post.ParentThread.AnswerPostID = Post.PostID) And objThread.ContainingForum.EnableForumsThreadStatus Then
                    RenderDivBegin(wr, "", "Forum_AnswerText") ' <span>
                    wr.Write(ForumControl.LocalizedText("AcceptedAnswer"))
                    wr.Write("&nbsp;")
                    RenderDivEnd(wr) ' </span>
                    ' If the thread is NOT answered AND this user started the post or is a moderator of some sort
                ElseIf ((CurrentForumUser.UserID = Post.ParentThread.StartedByUserID) Or (objSecurity.IsForumModerator)) And (Post.ParentThread.ThreadStatus = ThreadStatus.Unanswered) And objThread.ContainingForum.EnableForumsThreadStatus Then
                    ' Select the proper command argument (set before rendering)
                    If hsThreadAnswers.ContainsKey(Post.PostID) Then
                        cmdThreadAnswer = CType(hsThreadAnswers(Post.PostID), LinkButton)
                        cmdThreadAnswer.CommandArgument = Post.PostID.ToString
                        cmdThreadAnswer.RenderControl(wr)
                        wr.Write("&nbsp;")
                        wr.Write("&nbsp;")
                    End If
                    ' all that can be left worth displaying is if the post is the original, show the status icon
                Else
                    wr.Write("&nbsp;")
                End If
            End If
            RenderCellEnd(wr) ' </td> 

            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapRight", "")

            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td> 

            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>

            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>

            RenderRowBegin(wr) ' <tr>

            ' Author area
            RenderCellBegin(wr, authorCellClass, "", "20%", "center", "top", "1", "1")   ' <td>
            Me.RenderPostAuthor(wr, Post, PostCountIsEven)
            RenderCellEnd(wr) ' </td> 

            ' post area
            ' cell for post details (subject, buttons)
            RenderCellBegin(wr, bodyCellClass, "100%", "80%", "left", "top", "", "")      '<td>
            RenderPostHeader(wr, Post, PostCountIsEven)
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>
        End Sub

        ''' <summary>
        ''' Builds the left cell for RenderPost (author, rank, avatar area)
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="Post"></param>
        ''' <param name="PostCountIsEven"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderPostAuthor(ByVal wr As HtmlTextWriter, ByVal Post As PostInfo, ByVal PostCountIsEven As Boolean)
            If Not Post Is Nothing Then
                Dim author As ForumUserInfo = Post.Author
                Dim authorOnline As Boolean = (author.EnableOnlineStatus AndAlso author.IsOnline AndAlso (ForumControl.objConfig.EnableUsersOnline))
                Dim url As String

                ' table to display integrated media, user alias, poster rank, avatar, homepage, and number of posts.
                RenderTableBegin(wr, "", "Forum_PostAuthorTable", "", "100%", "0", "0", "", "", "")

                ' row to display user alias and online status
                RenderRowBegin(wr) '<tr> 

                'link to user profile, always display in both views
                If Not objConfig.EnableExternalProfile Then
                    url = author.UserCoreProfileLink
                Else
                    url = Utilities.Links.UserExternalProfileLink(author.UserID, objConfig.ExternalProfileParam, objConfig.ExternalProfilePage, objConfig.ExternalProfileUsername, author.Username)
                End If

                RenderCellBegin(wr, "", "", "", "", "middle", "", "") ' <td>

                ' display user online status
                If objConfig.EnableUsersOnline Then
                    RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "") ' <table>
                    RenderRowBegin(wr) ' <tr>
                    RenderCellBegin(wr, "", "", "", "", "middle", "", "")   ' <td> 
                    If authorOnline Then
                        RenderImage(wr, objConfig.GetThemeImageURL("s_online.") & objConfig.ImageExtension, ForumControl.LocalizedText("imgOnline"), "")
                    Else
                        RenderImage(wr, objConfig.GetThemeImageURL("s_offline.") & objConfig.ImageExtension, ForumControl.LocalizedText("imgOffline"), "")
                    End If
                    RenderCellEnd(wr) ' </td>

                    RenderCellBegin(wr, "", "", "", "", "middle", "", "")    ' <td>
                    wr.Write("&nbsp;")
                    RenderTitleLinkButton(wr, url, author.SiteAlias, "Forum_Profile", ForumControl.LocalizedText("ViewProfile"))
                    RenderCellEnd(wr) ' </td>

                    Dim objSecurity2 As New Forum.ModuleSecurity(ModuleID, TabID, -1, CurrentForumUser.UserID)

                    If objSecurity2.IsModerator Then
                        RenderCellBegin(wr, "", "", "", "", "middle", "", "")    ' <td>
                        wr.Write("&nbsp;")
                        RenderImageButton(wr, Utilities.Links.UCP_AdminLinks(TabID, ModuleID, author.UserID, UserAjaxControl.Profile), objConfig.GetThemeImageURL("s_edit.") & objConfig.ImageExtension, ForumControl.LocalizedText("EditProfile"), "")
                        RenderCellEnd(wr) ' </td>
                    End If

                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                Else
                    RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "") ' <table>
                    RenderRowBegin(wr) ' <tr>
                    RenderCellBegin(wr, "", "", "", "", "middle", "", "")   ' <td> 
                    RenderTitleLinkButton(wr, url, author.SiteAlias, "Forum_Profile", ForumControl.LocalizedText("ViewProfile"))
                    RenderCellEnd(wr) ' </td>

                    Dim objSecurity2 As New Forum.ModuleSecurity(ModuleID, TabID, -1, CurrentForumUser.UserID)

                    If objSecurity2.IsModerator Then
                        RenderCellBegin(wr, "", "", "", "", "middle", "", "")    ' <td>
                        wr.Write("&nbsp;")
                        RenderImageButton(wr, Utilities.Links.UCP_AdminLinks(TabID, ModuleID, author.UserID, UserAjaxControl.Profile), objConfig.GetThemeImageURL("s_edit.") & objConfig.ImageExtension, ForumControl.LocalizedText("EditProfile"), "")
                        RenderCellEnd(wr) ' </td>
                    End If

                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                End If

                RenderCellEnd(wr) ' </td>
                RenderRowEnd(wr) ' </tr> (end user alias/online)  

                ' display user ranking 
                If (objConfig.Ranking) Then
                    Dim authorRank As PosterRank = Utilities.ForumUtils.GetRank(author, ForumControl.objConfig)
                    Dim rankImage As String = String.Format("Rank_{0}." & objConfig.ImageExtension, CType(authorRank, Integer).ToString)
                    Dim rankURL As String = objConfig.GetThemeImageURL(rankImage)
                    Dim RankTitle As String = Utilities.ForumUtils.GetRankTitle(authorRank, objConfig)

                    RenderRowBegin(wr) ' <tr> (start ranking row)
                    RenderCellBegin(wr, "", "", "", "", "top", "", "") ' <td>
                    If objConfig.EnableRankingImage Then
                        RenderImage(wr, rankURL, RankTitle, "")
                    Else
                        RenderDivBegin(wr, "", "Forum_NormalSmall")
                        wr.Write(RankTitle)
                        RenderDivEnd(wr)
                    End If
                    RenderCellEnd(wr) ' </td>
                    RenderRowEnd(wr) ' </tr>
                End If

                ' display user avatar
                If objConfig.EnableUserAvatar AndAlso (String.IsNullOrEmpty(author.AvatarComplete) = False) Then
                    RenderRowBegin(wr) ' <tr> (start avatar row)
                    RenderCellBegin(wr, "Forum_UserAvatar", "", "", "", "top", "", "") ' <td>
                    wr.Write("<br />")
                    If objConfig.EnableUserAvatar And author.UserID > 0 Then
                        If Not author.IsSuperUser Then
                            Dim WebVisibility As UserVisibilityMode
                            WebVisibility = author.Profile.ProfileProperties(objConfig.AvatarProfilePropName).ProfileVisibility.VisibilityMode

                            Select Case WebVisibility
                                Case UserVisibilityMode.AdminOnly
                                    If objSecurity.IsForumAdmin Then
                                        RenderProfileAvatar(author, wr)
                                    End If
                                Case UserVisibilityMode.AllUsers
                                    RenderProfileAvatar(author, wr)
                                Case UserVisibilityMode.MembersOnly
                                    If CurrentForumUser.UserID > 0 Then
                                        RenderProfileAvatar(author, wr)
                                    End If
                            End Select
                        End If
                    Else
                        If author.UserID > 0 Then
                            RenderImage(wr, author.AvatarComplete, author.SiteAlias & "'s " & ForumControl.LocalizedText("Avatar"), "")
                        End If
                    End If

                    RenderCellEnd(wr) ' </td>
                    RenderRowEnd(wr) ' </tr>
                End If

                ' display system avatars (ie. DNN Core avatar)
                If objConfig.EnableSystemAvatar AndAlso (Not author.SystemAvatars = String.Empty) Then
                    Dim SystemAvatar As String
                    For Each SystemAvatar In author.SystemAvatarsComplete.Trim(";"c).Split(";"c)
                        If SystemAvatar.Length > 0 AndAlso (Not SystemAvatar.ToLower = "standard") Then
                            Dim SystemAvatarUrl As String = SystemAvatar
                            RenderRowBegin(wr) ' <tr> (start system avatar row) 
                            RenderCellBegin(wr, "Forum_NormalSmall", "", "", "", "top", "", "") ' <td>
                            wr.Write("<br />")
                            RenderImage(wr, SystemAvatarUrl, author.SiteAlias & "'s " & ForumControl.LocalizedText("Avatar"), "")
                            RenderCellEnd(wr) ' </td>
                            RenderRowEnd(wr) ' </tr>
                        End If
                    Next

                End If

                'Now for RoleBased Avatars
                If objConfig.EnableRoleAvatar AndAlso (Not author.RoleAvatar = ";") Then
                    Dim RoleAvatar As String
                    For Each RoleAvatar In author.RoleAvatarComplete.Trim(";"c).Split(";"c)
                        If RoleAvatar.Length > 0 AndAlso (Not RoleAvatar.ToLower = "standard") Then
                            Dim RoleAvatarUrl As String = RoleAvatar
                            RenderRowBegin(wr) ' <tr> (start system avatar row) 
                            RenderCellBegin(wr, "Forum_NormalSmall", "", "", "", "top", "", "") ' <td>
                            wr.Write("<br />")
                            RenderImage(wr, RoleAvatarUrl, author.SiteAlias & "'s " & ForumControl.LocalizedText("Avatar"), "")
                            RenderCellEnd(wr) ' </td>
                            RenderRowEnd(wr) ' </tr>
                        End If
                    Next
                End If

                'Author information
                RenderRowBegin(wr) ' <tr> 
                RenderCellBegin(wr, "Forum_NormalSmall", "", "", "", "top", "", "") ' <td>

                'Homepage
                If author.UserID > 0 Then
                    Dim WebSiteVisibility As UserVisibilityMode
                    WebSiteVisibility = author.Profile.ProfileProperties("Website").ProfileVisibility.VisibilityMode

                    Select Case WebSiteVisibility
                        Case UserVisibilityMode.AdminOnly
                            If objSecurity.IsForumAdmin Then
                                RenderWebSiteLink(author, wr)
                            End If
                        Case UserVisibilityMode.AllUsers
                            RenderWebSiteLink(author, wr)
                        Case UserVisibilityMode.MembersOnly
                            If CurrentForumUser.UserID > 0 Then
                                RenderWebSiteLink(author, wr)
                            End If
                    End Select

                    'Region
                    Dim CountryVisibility As UserVisibilityMode
                    CountryVisibility = author.Profile.ProfileProperties("Country").ProfileVisibility.VisibilityMode

                    Select Case CountryVisibility
                        Case UserVisibilityMode.AdminOnly
                            If objSecurity.IsForumAdmin Then
                                RenderCountry(author, wr)
                            End If
                        Case UserVisibilityMode.AllUsers
                            RenderCountry(author, wr)
                        Case UserVisibilityMode.MembersOnly
                            If CurrentForumUser.UserID > 0 Then
                                RenderCountry(author, wr)
                            End If
                    End Select
                End If

                'Joined
                Dim strJoinedDate As String
                Dim displayCreatedDate As DateTime = Utilities.ForumUtils.ConvertTimeZone(CType(author.Membership.CreatedDate, DateTime), objConfig)
                strJoinedDate = ForumControl.LocalizedText("Joined") & ": " & displayCreatedDate.ToShortDateString
                wr.Write("<br />" & strJoinedDate)

                'Post count
                RenderDivBegin(wr, "spAuthorPostCount", "Forum_NormalSmall")
                wr.Write(ForumControl.LocalizedText("PostCount").Replace("[PostCount]", author.PostCount.ToString))
                RenderDivEnd(wr)

                RenderCellEnd(wr) ' </td>
                RenderRowEnd(wr) ' </tr>
            End If

            RenderTableEnd(wr) ' </table>  (End of user avatar/alias table, close td next)
        End Sub

        ''' <summary>
        ''' Renders the user's profile avatar. 
        ''' </summary>
        ''' <param name="author"></param>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderProfileAvatar(ByVal author As ForumUserInfo, ByVal wr As HtmlTextWriter)
            ' This needs to be rendered w/ specified size
            If objConfig.EnableUserAvatar Then
                If author.ProfileAvatar <> String.Empty Then
                    Dim imgUserProfileAvatar As New Image
                    imgUserProfileAvatar.ImageUrl = author.AvatarComplete
                    imgUserProfileAvatar.RenderControl(wr)
                    imgUserProfileAvatar.Visible = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' Renders the user's website (as a link). 
        ''' </summary>
        ''' <param name="author"></param>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderWebSiteLink(ByVal author As ForumUserInfo, ByVal wr As HtmlTextWriter)
            If Len(author.UserWebsite) > 0 Then
                wr.Write("<br />")
                RenderLinkButton(wr, author.UserWebsite, Replace(author.UserWebsite, "http://", ""), "Forum_Profile", "", True, objConfig.NoFollowWeb)
            End If
        End Sub

        ''' <summary>
        ''' Renders the user's country. 
        ''' </summary>
        ''' <param name="author"></param>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderCountry(ByVal author As ForumUserInfo, ByVal wr As HtmlTextWriter)
            If objConfig.DisplayPosterRegion And Len(author.Profile.Region) > 0 Then
                wr.Write("<br />" & ForumControl.LocalizedText("Region") & ": " & author.Profile.Region)
            End If
        End Sub

        ''' <summary>
        ''' Builds the post details: subject, user location, edited, created date
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="Post"></param>
        ''' <param name="PostCountIsEven"></param>
        ''' <remarks></remarks>
        Private Sub RenderPostHeader(ByVal wr As HtmlTextWriter, ByVal Post As PostInfo, ByVal PostCountIsEven As Boolean)
            Dim detailCellClass As String = String.Empty
            Dim buttonCellClass As String = String.Empty
            Dim strSubject As String = String.Empty
            Dim strCreatedDate As String = String.Empty
            Dim strAuthorLocation As String = String.Empty
            Dim url As String = String.Empty

            If PostCountIsEven Then
                detailCellClass = "Forum_PostDetails"
                buttonCellClass = "Forum_PostButtons"
            Else
                detailCellClass = "Forum_PostDetails_Alt"
                buttonCellClass = "Forum_PostButtons_Alt"
            End If

            If ForumControl.objConfig.FilterSubject Then
                strSubject = Utilities.ForumUtils.FormatProhibitedWord(Post.Subject, Post.CreatedDate, PortalID)
            Else
                strSubject = Post.Subject
            End If

            'CP - Possible change for foreign culture date displays
            strCreatedDate = ForumControl.LocalizedText("PostedDateTime")
            Dim displayCreatedDate As DateTime = Utilities.ForumUtils.ConvertTimeZone(Post.CreatedDate, objConfig)
            strCreatedDate = strCreatedDate.Replace("[CreatedDate]", displayCreatedDate.ToString("dd MMM yy"))
            strCreatedDate = strCreatedDate.Replace("[PostTime]", displayCreatedDate.ToString("t"))

            ' display poster location 
            If (Not objConfig.DisplayPosterLocation = ShowPosterLocation.None) Then
                If ((objConfig.DisplayPosterLocation = ShowPosterLocation.ToAdmin) AndAlso (objSecurity.IsForumAdmin)) OrElse (objConfig.DisplayPosterLocation = ShowPosterLocation.ToAll) Then
                    If (Not Post.RemoteAddr.Length = 0) AndAlso (Not Post.RemoteAddr = "127.0.0.1") AndAlso (Not Post.RemoteAddr = "::1") Then
                        strAuthorLocation = String.Format("&nbsp;({0})", Utilities.ForumUtils.LookupCountry(Post.RemoteAddr))
                        ' This will show the ip in italics (This should only show to moderators) 
                        If objSecurity.IsForumModerator Then
                            strAuthorLocation = strAuthorLocation & "<EM> (" & Post.RemoteAddr & ")</EM>"
                        End If
                    End If
                End If
            End If
            'RenderTableBegin(wr, Post.PostID.ToString, "", "100%", "100%", "0", "0", "", "", "0") ' <table>
            RenderTableBegin(wr, "", "", "100%", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>

            RenderCellBegin(wr, "", "", "100%", "", "", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>

            RenderCellBegin(wr, detailCellClass, "", "100%", "left", "top", "", "") ' <td>

            '[skeel] Subject now works as a direct link to a specific post!
            RenderDivBegin(wr, "spCreatedDate", "Forum_Normal") ' <span>
            Me.RenderLinkButton(wr, Utilities.Links.ContainerViewPostLink(TabID, Post.ForumID, Post.PostID), strSubject, "Forum_NormalBold")
            wr.Write("&nbsp;")
            wr.Write(strAuthorLocation)

            ' display edited tag if post has been modified
            If (Post.UpdatedByUser > 0) Then
                ' if the person who edited the post is a moderator and hide mod edits is enabled, we don't want to show edit details.
                'CP - Impersonate
                Dim objPosterSecurity As New ModuleSecurity(ModuleID, TabID, ForumID, CurrentForumUser.UserID)
                If Not (objConfig.HideModEdits And objPosterSecurity.IsForumModerator) Then
                    wr.Write("&nbsp;")
                    RenderImage(wr, objConfig.GetThemeImageURL("s_edit.") & objConfig.ImageExtension, String.Format(ForumControl.LocalizedText("ModifiedBy") & " {0} {1}", Post.LastModifiedAuthor.SiteAlias, " " & ForumControl.LocalizedText("on") & " " & Post.UpdatedDate.ToString), "")
                End If
            End If

            RenderDivEnd(wr) ' </span> 

            RenderCellEnd(wr) ' </td> 

            'CP- Add back in row seperation 
            RenderRowEnd(wr) '</tr>    
            RenderRowBegin(wr) ' <tr>

            RenderCellBegin(wr, buttonCellClass, "", "", "left", "top", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>

            RenderCellBegin(wr, "", "", "5%", "left", "top", "", "") ' <td>
            ' '' display edited tag if post has been modified
            ''If (Post.UpdatedByUser > 0) Then
            ''	' if the person who edited the post is a moderator and hide mod edits is enabled, we don't want to show edit details.
            ''	'CP - Impersonate
            ''	Dim objPosterSecurity As New ModuleSecurity(ModuleID, TabID, ForumId, LoggedOnUser.UserID)
            ''	If Not (objConfig.HideModEdits And objPosterSecurity.IsForumModerator) Then
            ''		wr.Write("&nbsp;")
            ''		RenderImage(wr, objConfig.GetThemeImageURL("s_edit.") & objConfig.ImageExtension, String.Format(ForumControl.LocalizedText("ModifiedBy") & " {0} {1}", Post.LastModifiedAuthor.SiteAlias, " " & ForumControl.LocalizedText("on") & " " & Post.UpdatedDate.ToString), "")
            ''	End If
            ''End If
            RenderCellEnd(wr) ' </td> 

            ' (in flatview or selected, display commands on right)
            RenderCellBegin(wr, "", "", "95%", "right", "middle", "", "") ' <td>
            Me.RenderCommands(wr, Post)
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) '</tr>    
            RenderTableEnd(wr) ' </table> 
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) '</tr>    
            RenderTableEnd(wr) ' </table> 
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) '</tr>    

            RenderRowBegin(wr) ' <tr>

            Dim postBodyClass As String = String.Empty
            If PostCountIsEven Then
                postBodyClass = "Forum_PostBody"
            Else
                postBodyClass = "Forum_PostBody_Alt"
            End If

            RenderCellBegin(wr, postBodyClass, "100%", "80%", "left", "top", "", "") ' <td>
            Me.RenderPostBody(wr, Post, PostCountIsEven)
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>

            RenderTableEnd(wr) ' </table> 
        End Sub

        ''' <summary>
        ''' Renders the body of a post including signature and attachments
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="Post"></param>
        ''' <param name="PostCountIsEven"></param>
        ''' <remarks></remarks>
        Private Sub RenderPostBody(ByVal wr As HtmlTextWriter, ByVal Post As PostInfo, ByVal PostCountIsEven As Boolean)
            Dim author As ForumUserInfo = Post.Author
            Dim cleanBody As String = String.Empty
            Dim cleanSignature As String = String.Empty
            Dim attachmentClass As String = String.Empty
            Dim bodyForumText As Utilities.PostContent
            Dim url As String = String.Empty

            If Post.ParseInfo = PostParserInfo.None Or Post.ParseInfo = PostParserInfo.File Then
                'Nothing to Parse or just an Attachment not inline
                bodyForumText = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(Post.Body), objConfig)
            Else
                If Post.ParseInfo < PostParserInfo.Inline Then
                    'Something to parse, but not any inline instances
                    bodyForumText = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(Post.Body), objConfig, Post.ParseInfo)
                Else
                    'At lease Inline to Parse
                    If CurrentForumUser.UserID > 0 Then
                        bodyForumText = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(Post.Body), objConfig, Post.ParseInfo, Post.AttachmentCollection(objConfig.EnableAttachment), True)
                    Else
                        bodyForumText = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(Post.Body), objConfig, Post.ParseInfo, Post.AttachmentCollection(objConfig.EnableAttachment), False)
                    End If
                End If
            End If

            'We will NOT support emoticons or BBCode (quotes/code) in Signatures
            Dim Signature As Utilities.PostContent = New Utilities.PostContent(System.Web.HttpUtility.HtmlDecode(author.Signature), objConfig)

            If ForumControl.objConfig.EnableBadWordFilter Then
                cleanBody = Utilities.ForumUtils.FormatProhibitedWord(bodyForumText.ProcessHtml(), Post.CreatedDate, PortalID)
                cleanSignature = Utilities.ForumUtils.FormatProhibitedWord(Signature.ProcessHtml(), Post.CreatedDate, PortalID)
            Else
                cleanBody = bodyForumText.ProcessHtml()
                cleanSignature = Signature.ProcessHtml()
            End If

            If PostCountIsEven Then
                attachmentClass = "Forum_Attachments"
            Else
                attachmentClass = "Forum_Attachments_Alt"
            End If

            RenderTableBegin(wr, "tblPostBody" & Post.PostID.ToString, "", "100%", "100%", "0", "0", "left", "", "0") ' should be 0, contains all post body elements already taking max height
            ' row for post body
            RenderRowBegin(wr) '<Tr>
            ' cell for post body, set cell attributes           
            RenderCellBegin(wr, "", "", "100%", "left", "top", "", "") ' <td>

            RenderDivBegin(wr, "spBody", "Forum_Normal")    ' <div>
            wr.Write(cleanBody)
            RenderDivEnd(wr) ' </div>

            If objConfig.EnableUserSignatures Then
                ' insert signature if exists
                If Len(author.Signature) > 0 Then
                    RenderDivBegin(wr, "", "Forum_Normal")
                    wr.RenderBeginTag(HtmlTextWriterTag.Hr) ' <hr>
                    wr.RenderEndTag() ' </hr>
                    If objConfig.EnableHTMLSignatures Then
                        wr.Write(cleanSignature)
                    Else
                        wr.Write(cleanSignature)
                    End If
                    RenderDivEnd(wr) ' </span>
                End If
            End If

            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr> done with post body

            ' Report abuse
            RenderRowBegin(wr) '<tr> 
            'test bodycell
            RenderCellBegin(wr, "", "1px", "100%", "right", "", "", "") ' <td>

            If objConfig.EnablePostAbuse Then
                url = Utilities.Links.ReportToModsLink(TabID, ModuleID, Post.PostID)

                ' create table to hold link and image
                RenderTableBegin(wr, "", "", "", "", "0", "0", "", "middle", "0") ' <table>
                RenderRowBegin(wr) ' <tr>

                Dim renderSpace As Boolean = True

                If Post.PostReported > 0 Then
                    RenderCellBegin(wr, "", "", "", "right", "middle", "", "") ' <td>
                    ' make a link to take users to see whom reported this post and why
                    RenderImage(wr, objConfig.GetThemeImageURL("s_postabuse.") & objConfig.ImageExtension, Post.PostReported.ToString & " " & Localization.GetString("AbuseReports", ForumControl.objConfig.SharedResourceFile), "")
                    wr.Write("&nbsp;")
                    RenderCellEnd(wr) ' </td>
                    renderSpace = False
                End If

                If CurrentForumUser.UserID > 0 Then
                    RenderCellBegin(wr, "Forum_ReplyCell", "", "", "right", "middle", "", "") ' <td>
                    ' Warn link
                    RenderLinkButton(wr, url, ForumControl.LocalizedText("ReportAbuse"), "Forum_Link")
                    RenderCellEnd(wr) ' </td>
                    renderSpace = False
                End If

                If renderSpace Then
                    RenderCellBegin(wr, "", "", "", "right", "middle", "", "") ' <td>
                    wr.Write("&nbsp;")
                    RenderCellEnd(wr) ' </td>
                End If

                RenderRowEnd(wr) ' </tr> 
                RenderTableEnd(wr) ' </table>
            Else
                wr.Write("&nbsp;")
            End If

            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr> 

            ''CP-ADD - New per post rating (preparing UI) - Not Implemented
            'RenderRowBegin(wr) '<tr> 
            'RenderCellBegin(wr, postBodyClass, "100%", "100%", "", "", "", "")
            'RenderPerPostRating(wr)
            'RenderCellEnd(wr) ' </td>
            'RenderRowEnd(wr) ' </tr> done with perPostRating

            'New Attachments type
            'Select Case Post.ParseInfo
            'Case 4, 5, 6, 7, 15
            If objConfig.EnableAttachment AndAlso Post.AttachmentCollection(objConfig.EnableAttachment).Count > 0 Then
                RenderRowBegin(wr) '<tr> 
                RenderCellBegin(wr, attachmentClass, "1px", "100%", "left", "middle", "", "") ' <td>

                ' create table to hold link and image
                RenderTableBegin(wr, "", "", "", "", "0", "0", "", "middle", "0") ' <table>

                For Each objFile As AttachmentInfo In Post.AttachmentCollection(objConfig.EnableAttachment)
                    'Here we only handle attachments not inline type
                    If objFile.Inline = False Then

                        RenderRowBegin(wr) ' <tr>
                        RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>

                        Dim strlink As String
                        Dim strFileName As String

                        If (objConfig.AnonDownloads = False) Then
                            If HttpContext.Current.Request.IsAuthenticated = False Then
                                'AnonDownloads are Disabled
                                strFileName = Localization.GetString("NoAnonDownloads", ForumControl.objConfig.SharedResourceFile)

                                RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                                RenderImage(wr, objConfig.GetThemeImageURL("s_attachment.") & objConfig.ImageExtension, "", "")
                                RenderCellEnd(wr) ' </td>

                                RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                                wr.Write("&nbsp;")
                                wr.Write("<span class=Forum_NormalBold>" & strFileName & "</span>")
                                RenderCellEnd(wr) ' </td>

                                'We only want to display this information once..
                                RenderCellEnd(wr) ' </td>
                                RenderRowEnd(wr) ' </tr>
                                Exit For

                            Else
                                'User is Authenticated
                                strlink = FormatURL("FileID=" & objFile.FileID, False, True)
                                strFileName = objFile.LocalFileName

                                RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                                RenderImageButton(wr, objFile.FileName, objConfig.GetThemeImageURL("s_attachment.") & objConfig.ImageExtension, "", "", True)
                                RenderCellEnd(wr) ' </td>

                                RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                                wr.Write("&nbsp;")
                                RenderLinkButton(wr, strlink, strFileName, "Forum_Link", "", True, False)
                                RenderCellEnd(wr) ' </td>
                            End If

                        Else
                            'AnonDownloads are Enabled
                            strlink = FormatURL("FileID=" & objFile.FileID, False, True)
                            strFileName = objFile.LocalFileName

                            RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                            RenderImageButton(wr, strlink, objConfig.GetThemeImageURL("s_attachment.") & objConfig.ImageExtension, "", "", True)
                            RenderCellEnd(wr) ' </td>

                            RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderLinkButton(wr, strlink, strFileName, "Forum_Link", "", True, False)
                            RenderCellEnd(wr) ' </td>
                        End If

                        RenderCellEnd(wr) ' </td>
                        RenderRowEnd(wr) ' </tr> 
                    End If
                Next

                RenderTableEnd(wr) ' </table>
                RenderCellEnd(wr) ' </td>
                RenderRowEnd(wr) ' </tr> 

            End If
            'End Select
            RenderTableEnd(wr) ' </table> 
        End Sub

        ''' <summary>
        ''' Formats the URL used for attachments
        ''' </summary>
        ''' <param name="Link"></param>
        ''' <param name="TrackClicks"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Function FormatURL(ByVal Link As String, ByVal TrackClicks As Boolean) As String
            Return Common.Globals.LinkClick(Link, TabID, ModuleID, TrackClicks)
        End Function

        ''' <summary>
        ''' Formats the URL used for attachments (new version)
        ''' </summary>
        ''' <param name="Link"></param>
        ''' <param name="TrackClicks"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Function FormatURL(ByVal Link As String, ByVal TrackClicks As Boolean, ByVal ForceDownload As Boolean) As String
            Return Common.Globals.LinkClick(Link, TabID, ModuleID, TrackClicks, ForceDownload)
        End Function

        ''' <summary>
        ''' This allows for spacing between posts
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderSpacerRow(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<tr> 
            RenderCellBegin(wr, "Forum_SpacerRow", "", "", "", "", "", "")  ' <td>
            RenderImage(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderCellEnd(wr)

            RenderCellBegin(wr, "Forum_SpacerRow", "", "", "", "", "", "")  ' <td>
            RenderImage(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderCellEnd(wr) '</td>
            RenderRowEnd(wr) ' </tr>
        End Sub

        ''' <summary>
        ''' Footer w/ paging (second to last row)
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderFooter(ByVal wr As HtmlTextWriter)
            Dim pageCount As Integer = CInt(Math.Floor((objThread.Replies) / CurrentForumUser.PostsPerPage)) + 1
            Dim pageCountInfo As New StringBuilder

            pageCountInfo.Append(ForumControl.LocalizedText("PageCountInfo"))
            pageCountInfo.Replace("[PageNumber]", (PostPage + 1).ToString)
            pageCountInfo.Replace("[PageCount]", pageCount.ToString)

            ' Start the footer row
            RenderRowBegin(wr)
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "", "") ' <td><img/></td>

            RenderCellBegin(wr, "", "", "", "left", "middle", "", "") ' <td> 
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_FooterCapLeft", "") ' <td><img/></td>

            RenderCellBegin(wr, "Forum_Footer", "", "", "", "", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>

            RenderCellBegin(wr, "", "", "20%", "", "", "", "")  ' <td>
            RenderDivBegin(wr, "spPageCounting", "Forum_FooterText") ' <span>
            wr.Write("&nbsp;" & pageCountInfo.ToString)
            RenderDivEnd(wr) ' </span>
            RenderCellEnd(wr) ' </td> 

            RenderCellBegin(wr, "", "", "80%", "right", "", "", "")   ' <td> 
            If (pageCount > 1) Then
                RenderDivBegin(wr, "", "Forum_FooterText") ' <span>
                RenderPostPaging(wr, pageCount)
                wr.Write("&nbsp;")
                RenderDivEnd(wr) ' </span>
            End If

            ' Close paging
            RenderCellEnd(wr) ' </td>   
            RenderRowEnd(wr) ' </tr>   
            RenderTableEnd(wr) ' </table>  
            RenderCellEnd(wr) ' </td>   
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_FooterCapRight", "") ' <td><img/></td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "", "") ' <td><img/></td>
            RenderRowEnd(wr) ' </tr>  
        End Sub

        ''' <summary>
        ''' Renders the bottom prev/next buttons.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderBottomThreadButtons(ByVal wr As HtmlTextWriter)
            Dim url As String = String.Empty

            RenderRowBegin(wr) '<tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderCellBegin(wr, "", "", "100%", "", "", "", "") '<td>
            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("height_spacer.gif"), "", "")
            RenderRowEnd(wr) ' </tr>

            RenderRowBegin(wr) '<tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderCellBegin(wr, "", "", "100%", "", "", "", "") '<td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) '<tr>

            RenderCellBegin(wr, "", "", "50%", "left", "middle", "", "") ' <td> '
            ' new thread button
            'Remove LoggedOnUserID limitation if wishing to implement Anonymous Posting
            If (CurrentForumUser.UserID > 0) And (Not ForumID = -1) Then
                If Not objThread.ContainingForum.PublicPosting Then
                    If objSecurity.IsAllowedToStartRestrictedThread Then

                        RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                        RenderRowBegin(wr) '<tr>
                        url = Utilities.Links.NewThreadLink(TabID, ForumID, ModuleID)
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 

                        If CurrentForumUser.IsBanned Then
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link", False)
                        Else
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link")
                        End If

                        RenderCellEnd(wr) ' </Td>

                        If CurrentForumUser.IsBanned Or (Not objSecurity.IsAllowedToPostRestrictedReply) Or (objThread.IsClosed) Then
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                            RenderCellEnd(wr) ' </Td>
                        Else
                            url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        '[skeel] moved delete thread here
                        If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then
                            url = Utilities.Links.ThreadDeleteLink(TabID, ModuleID, ForumID, ThreadID, False)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("DeleteThread"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        RenderRowEnd(wr) ' </tr>
                        RenderTableEnd(wr) ' </table>
                    ElseIf objSecurity.IsAllowedToPostRestrictedReply Then
                        RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                        RenderRowBegin(wr) '<tr>

                        If CurrentForumUser.IsBanned Or objThread.IsClosed Then
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                            RenderCellEnd(wr) ' </Td>
                        Else
                            url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                            wr.Write("&nbsp;")
                            RenderCellEnd(wr) ' </Td>
                            RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                            RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                            RenderCellEnd(wr) ' </Td>
                        End If

                        RenderRowEnd(wr) ' </tr>
                        RenderTableEnd(wr) ' </table>
                    Else
                        wr.Write("&nbsp;")
                    End If
                Else
                    RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
                    RenderRowBegin(wr) '<tr>
                    url = Utilities.Links.NewThreadLink(TabID, ForumID, ModuleID)
                    RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                    If CurrentForumUser.IsBanned Then
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link", False)
                    Else
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("NewThread"), "Forum_Link")
                    End If
                    RenderCellEnd(wr) ' </Td>

                    If CurrentForumUser.IsBanned Or objThread.IsClosed Then
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                        RenderCellEnd(wr) ' </Td>
                    Else
                        url = Utilities.Links.NewPostLink(TabID, ForumID, objThread.ThreadID, "reply", ModuleID)
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                        RenderCellEnd(wr) ' </Td>
                    End If

                    '[skeel] moved delete thread here
                    If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then
                        url = Utilities.Links.ThreadDeleteLink(TabID, ModuleID, ForumID, ThreadID, False)
                        RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td>
                        wr.Write("&nbsp;")
                        RenderCellEnd(wr) ' </Td>
                        RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "middle", "", "") ' <td> 
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("DeleteThread"), "Forum_Link")
                        RenderCellEnd(wr) ' </Td>
                    End If

                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                End If
            End If

            RenderCellEnd(wr) ' </Td>

            RenderCellBegin(wr, "", "", "50%", "right", "", "", "") ' <td> ' 
            RenderTableBegin(wr, "", "", "100%", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>

            Dim PreviousEnabled As Boolean = False
            Dim EnabledText As String = "Disabled"
            If Not objThread.PreviousThreadID = 0 Then
                If Not objThread.IsPinned Then
                    PreviousEnabled = True
                    EnabledText = "Previous"
                End If
            End If

            If PreviousEnabled Then
                RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "", "", "")  ' <td> ' 
            Else
                RenderCellBegin(wr, "Forum_NavBarButtonDisabled", "", "", "", "", "", "")   ' <td> ' 
            End If

            RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>

            url = Utilities.Links.ContainerViewThreadLink(TabID, ForumID, objThread.PreviousThreadID)

            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> ' 

            If PreviousEnabled Then
                RenderLinkButton(wr, url, ForumControl.LocalizedText("Previous"), "Forum_Link")
            Else
                RenderDivBegin(wr, "", "Forum_NormalBold")
                wr.Write(ForumControl.LocalizedText("Previous"))
                RenderDivEnd(wr)
            End If
            RenderCellEnd(wr) ' </td>

            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>

            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> 
            wr.Write("&nbsp;")
            RenderCellEnd(wr) ' </td>

            'next button
            Dim NextEnabled As Boolean = False
            Dim NextText As String = "Disabled"
            If Not objThread.NextThreadID = 0 Then
                If Not objThread.IsPinned Then
                    NextEnabled = True
                    NextText = "Next"
                End If
            End If

            If NextEnabled Then
                RenderCellBegin(wr, "Forum_NavBarButton", "", "", "", "", "", "")  ' <td> 
            Else
                RenderCellBegin(wr, "Forum_NavBarButtonDisabled", "", "", "", "", "", "")   ' <td> 
            End If

            RenderTableBegin(wr, "", "", "", "", "0", "0", "", "", "0") '<Table>            
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "", "", "", "", "")  ' <td> 

            If NextEnabled Then
                url = Utilities.Links.ContainerViewThreadLink(TabID, ForumID, objThread.NextThreadID)
                RenderLinkButton(wr, url, ForumControl.LocalizedText("Next"), "Forum_Link")
            Else
                RenderDivBegin(wr, "", "Forum_NormalBold")
                wr.Write(ForumControl.LocalizedText("Next"))
                RenderDivEnd(wr)
            End If
            RenderCellEnd(wr) ' </td>   
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>

            ' enclosing table for prev/next
            wr.RenderEndTag() ' </Td>
            wr.RenderEndTag() ' </Tr>
            RenderTableEnd(wr) ' </table> 

            wr.RenderEndTag() ' </Td>
            wr.RenderEndTag() ' </Tr>
        End Sub

        ''' <summary>
        ''' Renders the bottom breadcrumb.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderBottomBreadCrumb(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<Tr>

            RenderCellBegin(wr, "", "", "", "left", "", "2", "") ' <td> 
            Dim ChildGroupView As Boolean = False
            If CType(ForumControl.TabModuleSettings("groupid"), String) <> String.Empty Then
                ChildGroupView = True
            End If
            wr.Write(Utilities.ForumUtils.BreadCrumbs(TabID, ModuleID, ForumScope.Posts, objThread, objConfig, ChildGroupView))
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr> 
        End Sub

        ''' <summary>
        ''' Renders the tags area, which is blow the bottom breadcrumb. 
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks>We are only allowing tagging in public forums, because of security concerns in tag search results (we go one level deeper in perms than core).</remarks>
        Private Sub RenderTags(ByVal wr As HtmlTextWriter)
            If objThread.ContainingForum.PublicView AndAlso objConfig.EnableTagging Then
                RenderRowBegin(wr) '<tr>

                RenderCellBegin(wr, "", "", "98%", "left", "", "2", "") ' <td> 
                tagsControl.RenderControl(wr)
                RenderCellEnd(wr) ' </td> 

                If objSecurity.IsForumModerator Then
                    ' reserved for an edit button, or control, to manage tags.
                    'RenderCellBegin(wr, "", "", "5%", "left", "", "", "")	' <td> 
                    'tagsControl.RenderControl(wr)
                    'RenderCellEnd(wr) ' </td> 
                End If

                RenderRowEnd(wr) ' </tr>  
            End If
        End Sub

        ''' <summary>
        ''' Determines if we should render the quick reply section based on several conditions, also adds a bottom row for padding.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderQuickReply(ByVal wr As HtmlTextWriter)
            If objConfig.EnableQuickReply Then
                If (CurrentForumUser.UserID > 0) And (Not objThread.ForumID = -1) Then
                    If Not objThread.ContainingForum.PublicPosting Then
                        If CurrentForumUser.IsBanned = False And objThread.IsClosed = False Then
                            If objSecurity.IsAllowedToPostRestrictedReply Then
                                QuickReply(wr)
                            End If
                        End If
                    Else
                        If CurrentForumUser.IsBanned = False And objThread.IsClosed = False Then
                            QuickReply(wr)
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Renders the bottom area that includes date drop down, view subscribers link (for admin) and notification checkbox.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub RenderThreadOptions(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "100%", "right", "", "2", "") ' <td> 

            If PostCollection.Count > 0 Then
                wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                wr.RenderEndTag() ' </Img>
                ddlViewDescending.RenderControl(wr)
            End If

            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>   

            ' Notifications row
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "", "right", "", "2", "")   ' <td> 
            wr.Write("<br />")

            ' Display tracking option if user is authenticated and post count > 0 and user not track parent forum (make sure tracking is enabled)
            'CP - Seperating so we can show user they are tracking at forum level if need be
            If (PostCollection.Count > 0) AndAlso (CurrentForumUser.UserID > 0) And (objConfig.MailNotification) Then
                If objSecurity.IsForumAdmin Then
                    cmdThreadSubscribers.RenderControl(wr)
                    wr.Write("<br />")
                End If

                If TrackedForum Then
                Else
                    chkEmail.RenderControl(wr)
                End If
            End If

            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>

            'Close the table
            RenderTableEnd(wr) ' </table> 

            RenderCellEnd(wr) ' </td> 
            RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "")
            RenderRowEnd(wr) ' </tr>   

            ' render bottom spacer row
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td> 
            RenderCellEnd(wr) ' </td> 
            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td> 
            wr.Write("<br />")
            RenderCellEnd(wr) ' </td> 
            RenderCellBegin(wr, "", "", "", "", "", "", "") ' <td> 
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>  
        End Sub

        ''' <summary>
        ''' Renders available post reply/quote/moderate, etc.  buttons
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="Post"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderCommands(ByVal wr As HtmlTextWriter, ByVal Post As PostInfo)
            Dim author As ForumUserInfo = Post.Author
            Dim url As String = String.Empty

            ' Render reply/mod buttons if necessary
            ' First see if the user has the ability to post
            ' remove logged on limitation if wishing to implement Anonymous posting
            If CurrentForumUser.UserID > 0 Then
                If (Not objThread.ContainingForum.PublicPosting And objSecurity.IsAllowedToPostRestrictedReply) Or (objThread.ContainingForum.PublicPosting = True) Then
                    ' move link (logged user is admin and this is the first post in the thread)
                    'start table for mod/reply buttons
                    RenderTableBegin(wr, "tblCommand_" & Post.PostID.ToString, "", "", "", "4", "0", "", "", "0")     ' <table>
                    RenderRowBegin(wr)

                    'Never Remove LoggedOnUserID limitation EVEN if wishing to implement Anonymous Posting - ParentPostID is so we know this is the first post in a thread to move it
                    If CurrentForumUser.UserID > 0 And (objSecurity.IsForumModerator) AndAlso (Post.ParentPostID = 0) Then
                        url = Utilities.Links.ThreadMoveLink(TabID, ModuleID, ForumID, ThreadID)

                        RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Move"), "Forum_Link")
                        RenderCellEnd(wr)
                    ElseIf CurrentForumUser.UserID > 0 And (objSecurity.IsForumModerator) Then
                        ' Split thread
                        url = Utilities.Links.ThreadSplitLink(TabID, ModuleID, ForumID, Post.PostID)

                        RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Split"), "Forum_Link")
                        RenderCellEnd(wr)
                    End If

                    'Never Remove LoggedOnUserID limitation EVEN if wishing to implement Anonymous Posting
                    If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then
                        url = Utilities.Links.PostDeleteLink(TabID, ModuleID, ForumID, Post.PostID, False)

                        RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Delete"), "Forum_Link")
                        RenderCellEnd(wr)
                    End If

                    'Never Remove LoggedOnUserID limitation EVEN if wishing to implement Anonymous Posting - Anonymous cannot edit post
                    If CurrentForumUser.UserID > 0 AndAlso (objSecurity.IsForumModerator) Then
                        url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "edit", ModuleID)

                        RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                        RenderLinkButton(wr, url, ForumControl.LocalizedText("Edit"), "Forum_Link")
                        RenderCellEnd(wr)
                        'don't allow non mod, forum admin or anything other than a moderator to edit a closed forum post (if the forum is not moderated, or the user is trusted)
                    ElseIf CurrentForumUser.UserID > 0 And (Post.ParentThread.ContainingForum.IsActive) And ((CurrentForumUser.UserID = Post.Author.UserID) AndAlso (Post.ParentThread.ContainingForum.IsModerated = False Or author.IsTrusted Or objSecurity.IsUnmoderated)) Then

                        '[skeel] check for PostEditWindow
                        If objConfig.PostEditWindow = 0 Then
                            url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "edit", ModuleID)
                            RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")

                            If CurrentForumUser.IsBanned Then
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Edit"), "Forum_Link", False)
                            Else
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Edit"), "Forum_Link")
                            End If

                            RenderCellEnd(wr)
                        Else
                            If Post.CreatedDate.AddMinutes(CDbl(objConfig.PostEditWindow)) > Now Then
                                url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "edit", ModuleID)
                                RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")

                                If CurrentForumUser.IsBanned Then
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Edit"), "Forum_Link", False)
                                Else
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Edit"), "Forum_Link")
                                End If

                                RenderCellEnd(wr)
                            End If
                        End If
                    End If

                    'First check if the thread is opened, if not then handle for single situation
                    If CurrentForumUser.UserID > 0 AndAlso (Not Post.ParentThread.IsClosed) And (Post.ParentThread.ContainingForum.IsActive) Then
                        If Not Post.ParentThread.ContainingForum.PublicPosting Then
                            ' see if user can reply
                            If objSecurity.IsAllowedToPostRestrictedReply Then
                                url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "quote", ModuleID)
                                ' Quote link
                                RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                                If CurrentForumUser.IsBanned Then
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Quote"), "Forum_Link", False)
                                Else
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Quote"), "Forum_Link")
                                End If
                                RenderCellEnd(wr)

                                url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "reply", ModuleID)

                                ' Reply link                    
                                RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                                If CurrentForumUser.IsBanned Then
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                                Else
                                    RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                                End If
                                RenderCellEnd(wr)
                            End If
                        Else
                            url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "quote", ModuleID)
                            ' Quote link
                            RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                            If CurrentForumUser.IsBanned Then
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Quote"), "Forum_Link", False)
                            Else
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Quote"), "Forum_Link")
                            End If
                            RenderCellEnd(wr)

                            url = Utilities.Links.NewPostLink(TabID, ForumID, Post.PostID, "reply", ModuleID)

                            ' Reply link                    
                            RenderCellBegin(wr, "Forum_ReplyCell", "", "", "", "", "", "")
                            If CurrentForumUser.IsBanned Then
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link", False)
                            Else
                                RenderLinkButton(wr, url, ForumControl.LocalizedText("Reply"), "Forum_Link")
                            End If
                            RenderCellEnd(wr)
                        End If
                    End If

                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                Else
                    ' User cannot post, which means no moderation either
                    RenderTableBegin(wr, "tblCommand_" & Post.PostID.ToString, "", "", "", "4", "0", "", "", "0")     ' <table>
                    RenderRowBegin(wr)
                    RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "left")
                    RenderRowEnd(wr) ' </tr>
                    RenderTableEnd(wr) ' </table>
                End If
            Else
                ' User cannot post, which means no moderation either
                RenderTableBegin(wr, "tblCommand_" & Post.PostID.ToString, "", "", "", "4", "0", "", "", "0")     ' <table>
                RenderRowBegin(wr)
                RenderCapCell(wr, objConfig.GetThemeImageURL("spacer.gif"), "", "left")
                RenderRowEnd(wr) ' </tr>
                RenderTableEnd(wr) ' </table>
            End If
        End Sub

        ''' <summary>
        ''' Determins if post is even or odd numbered row
        ''' </summary>
        ''' <param name="Count"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        Private Function ThreadIsEven(ByVal Count As Integer) As Boolean
            If Count Mod 2 = 0 Then
                'even
                Return True
            Else
                'odd
                Return False
            End If
        End Function

        ''' <summary>
        ''' Just relevant to paging
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="PageCount"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderPostPaging(ByVal wr As HtmlTextWriter, ByVal PageCount As Integer)
            ' First, previous, next, last thread hyperlinks
            Dim backwards As Boolean
            Dim forwards As Boolean
            Dim url As String = String.Empty

            If PostPage <> 0 Then
                backwards = True
            End If

            If PostPage <> PageCount - 1 Then
                forwards = True
            End If

            If (backwards) Then
                ' < Previous 
                url = Utilities.Links.ContainerViewThreadPagedLink(TabID, ForumID, ThreadID, PostPage)
                wr.AddAttribute(HtmlTextWriterAttribute.Href, url)
                wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                wr.RenderBeginTag(HtmlTextWriterTag.A) '<a>
                wr.Write(ForumControl.LocalizedText("Previous"))
                wr.RenderEndTag() ' </A>

                wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                wr.RenderEndTag() ' </Img>
            End If

            ' If thread spans several pages, then display text like (Page 1, 2, 3, ..., 5)
            Dim displayPage As Integer = PostPage + 1
            Dim startCap As Integer = Math.Max(4, displayPage - 1)
            Dim endCap As Integer = Math.Min(PageCount - 1, displayPage + 1)
            Dim sepStart As Boolean = False
            Dim sepEnd As Boolean = False
            Dim iPost As Integer

            For iPost = 1 To PageCount
                url = Utilities.Links.ContainerViewThreadPagedLink(TabID, ForumID, ThreadID, iPost)

                If iPost <= 3 Then
                    If iPost <> displayPage Then
                        wr.AddAttribute(HtmlTextWriterAttribute.Href, url)
                    End If
                    wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                    wr.RenderBeginTag(HtmlTextWriterTag.A) '<a>
                    wr.Write(iPost)
                    wr.RenderEndTag() ' </A>

                    wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                    wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                    wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                    wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                    wr.RenderEndTag() ' </Img>
                End If

                If (iPost > 3 AndAlso iPost < startCap) AndAlso (Not sepStart) Then
                    wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_Link")
                    wr.AddAttribute(HtmlTextWriterAttribute.Id, "spStartCap")
                    wr.RenderBeginTag(HtmlTextWriterTag.Span) ' <span>
                    wr.Write("...")
                    wr.RenderEndTag() ' </Span>
                    sepStart = True
                End If

                If iPost >= startCap AndAlso iPost <= endCap Then
                    If iPost <> displayPage Then
                        'wr.AddAttribute(HtmlTextWriterAttribute.Href, GetURL(Document, Page, String.Format("threadpage={0}", iPost), "postid=&action="))
                        wr.AddAttribute(HtmlTextWriterAttribute.Href, url)
                    End If
                    wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                    wr.RenderBeginTag(HtmlTextWriterTag.A)
                    wr.Write(iPost)
                    wr.RenderEndTag() ' A

                    wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                    wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                    wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                    wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                    wr.RenderEndTag() ' </Img>
                End If

                If (iPost > 3) AndAlso (iPost > endCap AndAlso iPost < PageCount) AndAlso (Not sepEnd) Then
                    wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                    wr.AddAttribute(HtmlTextWriterAttribute.Id, "spEndCap")
                    wr.RenderBeginTag(HtmlTextWriterTag.Span) '<span>
                    wr.Write("...")
                    wr.RenderEndTag() ' </Span>
                    sepEnd = True
                End If

                If iPost = PageCount AndAlso iPost > 3 Then
                    If iPost <> displayPage Then
                        wr.AddAttribute(HtmlTextWriterAttribute.Href, url)
                    End If
                    wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                    wr.RenderBeginTag(HtmlTextWriterTag.A) ' <a>
                    wr.Write(iPost)
                    wr.RenderEndTag() ' </A>

                    wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                    wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                    wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                    wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                    wr.RenderEndTag() ' </Img>
                End If
            Next

            If (forwards) Then
                ' Next >
                url = Utilities.Links.ContainerViewThreadPagedLink(TabID, ForumID, ThreadID, PostPage + 2)
                wr.AddAttribute(HtmlTextWriterAttribute.Href, url)
                wr.AddAttribute(HtmlTextWriterAttribute.Class, "Forum_FooterText")
                wr.RenderBeginTag(HtmlTextWriterTag.A) '<a>
                wr.Write(ForumControl.LocalizedText("Next"))
                wr.RenderEndTag() ' </A>

                wr.AddAttribute(HtmlTextWriterAttribute.Border, "0")
                wr.AddAttribute(HtmlTextWriterAttribute.Src, objConfig.GetThemeImageURL("spacer.gif"))
                wr.AddAttribute(HtmlTextWriterAttribute.Alt, "")
                wr.RenderBeginTag(HtmlTextWriterTag.Img) ' <Img>
                wr.RenderEndTag() ' </Img>
            End If
        End Sub

        ''' <summary>
        ''' Builds a bookmark for RenderPost, used to navigate directly to a specific post
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <param name="BookMark"></param>
        ''' <remarks>
        ''' </remarks>
        Private Sub RenderPostBookmark(ByVal wr As HtmlTextWriter, ByVal BookMark As String)
            wr.Write("<a name=""" & BookMark & """></a>")
        End Sub

        ''' <summary>
        ''' Renders a textbox on the screen for a quickly reply to threads.
        ''' </summary>
        ''' <param name="wr"></param>
        ''' <remarks></remarks>
        Private Sub QuickReply(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "100%", "left", "middle", "2", "") ' <td> 
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapLeft", "") ' <td><img/></td>
            RenderCellBegin(wr, "Forum_Header", "", "100%", "", "", "", "") ' <td>
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "100%", "", "", "", "")  ' <td>
            RenderDivBegin(wr, "", "Forum_HeaderText") ' <span>
            wr.Write("&nbsp;" & "Quick Reply")
            RenderDivEnd(wr) ' </span>
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>   
            RenderTableEnd(wr) ' </table>  
            RenderCellEnd(wr) ' </td>   
            RenderCapCell(wr, objConfig.GetThemeImageURL("headfoot_height.gif"), "Forum_HeaderCapRight", "") ' <td><img/></td>
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>  

            ' Show quick reply textbox row
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "Forum_UCP_HeaderInfo", "", "", "left", "middle", "2", "") ' <td> 
            RenderTableBegin(wr, "", "", "", "100%", "0", "0", "", "", "0") ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "", "", "125px", "", "top", "", "") ' <td>
            RenderDivBegin(wr, "", "Forum_NormalBold") ' <span>
            wr.Write("&nbsp;" & "Body")
            RenderDivEnd(wr) ' </span>
            RenderCellEnd(wr) ' </td> 
            RenderCellBegin(wr, "", "", "", "left", "", "", "") ' <td>
            txtQuickReply.RenderControl(wr)
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td>
            RenderRowEnd(wr) ' </tr>  

            ' Submit Row
            RenderRowBegin(wr) '<tr>
            RenderCellBegin(wr, "", "", "", "center", "middle", "2", "")    ' <td> 
            RenderTableBegin(wr, "", "", "", "125px", "0", "0", "", "", "0")    ' <table>
            RenderRowBegin(wr) ' <tr>
            RenderCellBegin(wr, "Forum_NavBarButton", "", "125px", "", "", "", "")  ' <td>
            cmdSubmit.RenderControl(wr)
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>
            RenderTableEnd(wr) ' </table>
            RenderCellEnd(wr) ' </td> 
            RenderRowEnd(wr) ' </tr>  
        End Sub

        ''' <summary>
        ''' Renders structure of Advertisement content
        ''' </summary>
        ''' <history>
        ''' 	[b.waluszko]	21/10/2010	Created
        ''' </history>
        Private Sub RenderAdvertisementPost(ByVal wr As HtmlTextWriter)
            RenderRowBegin(wr) ' <tr>
            wr.Write("<td class=""AdvertisementPost"" colspan=""2"">")
            wr.Write(objConfig.AdvertisementText)

            'check if there are some banners to render
            Dim advertController As New AdvertController
            Dim bannerController As New Vendors.BannerController

            Dim adverts As IEnumerable(Of AdvertInfo)
            adverts = advertController.VendorsGet(Me.ModuleID).Where(Function(ad) ad.IsEnabled = True)

            'first check vendors
            If (adverts IsNot Nothing) AndAlso adverts.Count > 0 Then
                wr.Write("<br/>")
                For Each advert As AdvertInfo In adverts
                    Dim banners As List(Of Vendors.BannerInfo)

                    'second check banners connected to vendors
                    banners = advertController.BannersGet(advert.VendorId)
                    If (banners IsNot Nothing) AndAlso banners.Count > 0 Then
                        For Each b As Vendors.BannerInfo In banners
                            advertController.BannerViewIncrement(b.BannerId)
                            Dim fileInfo As DotNetNuke.Services.FileSystem.FileInfo = CType(FileManager.Instance.GetFile(Integer.Parse(b.ImageFile.Split(Char.Parse("="))(1))), Services.FileSystem.FileInfo)
                            wr.Write(bannerController.FormatBanner(advert.VendorId, b.BannerId, b.BannerTypeId, b.BannerName, fileInfo.RelativePath, b.Description, b.URL, b.Width, b.Height, "L", objConfig.CurrentPortalSettings.HomeDirectory) & "&nbsp;")
                        Next

                    End If

                Next
            End If

            wr.Write("</td>")
            RenderRowEnd(wr) ' </tr>
        End Sub

#End Region

    End Class

End Namespace
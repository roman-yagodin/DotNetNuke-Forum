﻿'
' DotNetNuke® - http://www.dotnetnuke.com
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

Imports DotNetNuke.Services.FileSystem
Imports System.IO
Imports DotNetNuke.Forum.Library

Namespace DotNetNuke.Modules.Forum

    ''' <summary>
    ''' The purpose of the PostConnector class is to have a centralized spot where multiple areas within the Forum Module can post to the database. This also is abstracted so third party providers, such as metaPost, can easily post messages. This also paves the way for Quick Reply as well as SMTP replies. 
    ''' </summary>
    ''' <remarks>All functions should be pointed to PostToDatabase in the end.</remarks>
    Public Class PostConnector

#Region "Private Properties"

        Private _objConfig As Forum.Configuration

        Private Property objConfig() As Forum.Configuration
            Get
                Return _objConfig
            End Get
            Set(ByVal value As Forum.Configuration)
                _objConfig = value
            End Set
        End Property

        Private regCounter As Integer = 0

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' All posting from external sources are designed to go through this method. The API will take care of all security, moderation, notifications, filtering, etc. This is out of date.
        ''' </summary>
        ''' <param name="TabID">The TabID (page) your forum that you wish to post to resides on.</param>
        ''' <param name="ModuleID">The module you wish to post to.</param>
        ''' <param name="PortalID">The portal associated with the tab/modue and user posting.</param>
        ''' <param name="UserID">The user posting the message.</param>
        ''' <param name="PostSubject">The 'uncleansed' post subject.</param>
        ''' <param name="PostBody">The 'uncleansed' post body.</param>
        ''' <param name="ForumID">The forum you wish to post to.</param>
        ''' <param name="ParentPostID">The post you are replying to, if you are starting a new thread this should be equal to 0.</param>
        ''' <param name="Attachments">A string of attachments, separated by a semicolon (Not fully implemented).</param>
        ''' <param name="Provider">A unique string used to identify how the post was submitted (this should be your own unique value for your module/task).</param>
        ''' <returns>An enumerator PostMessage that tells what happend (post moderated, post approved, reason rejected, etc.).</returns>
        ''' <remarks>This is available for all outside modules/applications to post to the forum module.</remarks>
        Public Function SubmitExternalPost(ByVal TabID As Integer, ByVal ModuleID As Integer, ByVal PortalID As Integer, ByVal UserID As Integer, ByVal PostSubject As String, ByVal PostBody As String, ByVal ForumID As Integer, ByVal ParentPostID As Integer, ByVal Attachments As String, ByVal Provider As String) As PostMessage
            Return PostingValidation(TabID, ModuleID, PortalID, UserID, PostSubject, PostBody, ForumID, ParentPostID, -1, False, False, False, Forum.ThreadStatus.NotSet, Attachments, "0.0.0.0", -1, False, Provider, -1, Nothing)
        End Function

        ''' <summary>
        ''' All posting from external sources are designed to go through this method. The API will take care of all security, moderation, notifications, filtering, etc,
        ''' </summary>
        ''' <param name="TabID">The TabID (page) your forum that you wish to post to resides on.</param>
        ''' <param name="ModuleID">The module you wish to post to.</param>
        ''' <param name="PortalID">The portal associated with the tab/modue and user posting.</param>
        ''' <param name="UserID">The user posting the message.</param>
        ''' <param name="PostSubject">The 'uncleansed' post subject.</param>
        ''' <param name="PostBody">The 'uncleansed' post body.</param>
        ''' <param name="ForumID">The forum you wish to post to.</param>
        ''' <param name="ParentPostID">The post you are replying to, if you are starting a new thread this should be equal to 0.</param>
        ''' <param name="Attachments">A string of attachments, separated by a semicolon (Not fully implemented).</param>
        ''' <param name="Provider">A unique string used to identify how the post was submitted (this should be your own unique value for your module/task).</param>
        ''' <returns>An enumerator PostMessage that tells what happend (post moderated, post approved, reason rejected, etc.).</returns>
        ''' <remarks>This is available for all outside modules/applications to post to the forum module.</remarks>
        Public Function SubmitExternalPost(ByVal TabID As Integer, ByVal ModuleID As Integer, ByVal PortalID As Integer, ByVal UserID As Integer, ByVal PostSubject As String, ByVal PostBody As String, ByVal ForumID As Integer, ByVal ParentPostID As Integer, ByVal Attachments As String, ByVal Provider As String, ByVal ParentThreadID As Integer, ByVal Terms As List(Of DotNetNuke.Entities.Content.Taxonomy.Term)) As PostMessage
            Return PostingValidation(TabID, ModuleID, PortalID, UserID, PostSubject, PostBody, ForumID, ParentPostID, -1, False, False, False, Forum.ThreadStatus.NotSet, Attachments, "0.0.0.0", -1, False, Provider, ParentThreadID, Terms)
        End Function

        ''' <summary>
        ''' This is meant for preview only and is not required prior to any post submissions (although, all posts will run through this when submitted).
        ''' </summary>
        ''' <param name="PostBody"></param>
        ''' <param name="myConfig"></param>
        ''' <param name="PortalID"></param>
        ''' <param name="objAction"></param>
        ''' <param name="UserID"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ProcessPostBody(ByVal PostBody As String, ByVal myConfig As Forum.Configuration, ByVal PortalID As Integer, ByVal objAction As PostAction, ByVal UserID As Integer) As String
            Dim objSecurity As New PortalSecurity
            Dim ProcessedBody As String
            Dim fText As Utilities.PostContent

            objConfig = myConfig

            ProcessedBody = objSecurity.InputFilter(PostBody, PortalSecurity.FilterFlag.NoScripting)

            If objConfig.EnableBadWordFilter Then
                Dim cntFilter As New WordFilterController
                ProcessedBody = cntFilter.FilterBadWord(PostBody, PortalID)
            End If

            ProcessedBody = HttpUtility.HtmlDecode(ProcessedBody)

            If objConfig.EnableAttachment = True And ProcessedBody.ToLower.IndexOf("[attachment]") >= 0 Then
                Dim lstAttachments As List(Of AttachmentInfo) = GetPreviewAttachments(ProcessedBody, objAction, UserID)

                fText = New Utilities.PostContent(ProcessedBody, objConfig, PostParserInfo.BBCode + PostParserInfo.Inline, lstAttachments, True)
            Else
                fText = New Utilities.PostContent(ProcessedBody, objConfig, PostParserInfo.BBCode)
            End If

            If objConfig.DisableHTMLPosting Then
                ProcessedBody = fText.ProcessPlainText(objConfig)
                ProcessedBody = Utilities.ForumUtils.StripHTML(ProcessedBody)
            Else
                ProcessedBody = fText.ProcessHtml()

                ' [skeel] Check all images inside post for maxwidth
                Dim regExp As Regex = New Regex("<img([^>]+)>")
                ProcessedBody = regExp.Replace(ProcessedBody, New MatchEvaluator(AddressOf ReplaceImageUrl))
                ' [skeel] Check all links inside post 
                regExp = New Regex("<a[^>]+>[^>]*<\/a>")
                ProcessedBody = regExp.Replace(ProcessedBody, New MatchEvaluator(AddressOf ReplaceUrls))

                ProcessedBody = HttpUtility.HtmlEncode(ProcessedBody)
            End If

            Return ProcessedBody
        End Function

        ''' <summary>
        ''' This is meant for preview only and is not required prior to any post submissions (although, all posts will run through this when submitted).
        ''' </summary>
        ''' <param name="PostSubject"></param>
        ''' <param name="objConfig"></param>
        ''' <param name="PortalID"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ProcessPostSubject(ByVal PostSubject As String, ByVal objConfig As Forum.Configuration, ByVal PortalID As Integer) As String
            Dim objSecurity As New PortalSecurity
            Dim ProcessedSubject As String

            ProcessedSubject = objSecurity.InputFilter(PostSubject, PortalSecurity.FilterFlag.NoScripting)

            If objConfig.EnableBadWordFilter And objConfig.FilterSubject Then
                Dim cntFilter As New WordFilterController
                ProcessedSubject = cntFilter.FilterBadWord(PostSubject, PortalID)
            End If

            ProcessedSubject = HttpUtility.HtmlDecode(ProcessedSubject)

            If Not objConfig.DisableHTMLPosting Then
                ProcessedSubject = HttpUtility.HtmlEncode(ProcessedSubject)
            End If

            Return ProcessedSubject
        End Function

#End Region

#Region "Friend Methods"

        ''' <summary>
        ''' This is used to submit a post to a specific forum from within the Forum module. 
        ''' </summary>
        ''' <param name="TabID"></param>
        ''' <param name="ModuleID"></param>
        ''' <param name="PortalID"></param>
        ''' <param name="UserID"></param>
        ''' <param name="PostSubject"></param>
        ''' <param name="PostBody"></param>
        ''' <param name="ForumID"></param>
        ''' <param name="ParentPostID"></param>
        ''' <param name="PostID"></param>
        ''' <param name="IsPinned"></param>
        ''' <param name="IsClosed"></param>
        ''' <param name="Status"></param>
        ''' <param name="AttachmentFileIDs">A string of comma separated integers(DNN File ID's). It is assumed that all attachments (and local post images) already exist within the DNN file system.</param>
        ''' <param name="RemoteAddress"></param>
        ''' <param name="PollID"></param>
        ''' <param name="IsQuote"></param>
        ''' <param name="ThreadID"></param>
        ''' <returns></returns>
        ''' <remarks>This is set as friend so only PostEdit uses this method. We want to avoid threadstatus, polls, thread icon changes from external sources (for now). Also avoiding post edits externally,.</remarks>
        Friend Function SubmitInternalPost(ByVal TabID As Integer, ByVal ModuleID As Integer, ByVal PortalID As Integer, ByVal UserID As Integer, ByVal PostSubject As String, ByVal PostBody As String, ByVal ForumID As Integer, ByVal ParentPostID As Integer, ByVal PostID As Integer, ByVal IsPinned As Boolean, ByVal IsClosed As Boolean, ByVal ReplyNotify As Boolean, ByVal Status As Forum.ThreadStatus, ByVal AttachmentFileIDs As String, ByVal RemoteAddress As String, ByVal PollID As Integer, ByVal IsQuote As Boolean, ByVal ThreadID As Integer, ByVal Terms As List(Of DotNetNuke.Entities.Content.Taxonomy.Term)) As PostMessage
            Return PostingValidation(TabID, ModuleID, PortalID, UserID, PostSubject, PostBody, ForumID, ParentPostID, PostID, IsPinned, IsClosed, ReplyNotify, Status, AttachmentFileIDs, RemoteAddress, PollID, IsQuote, "InterModule", ThreadID, Terms)
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' All posting goes through this method, it handles all permissions and validation then passes it on to the internal posting method.
        ''' </summary>
        ''' <param name="TabID"></param>
        ''' <param name="ModuleID"></param>
        ''' <param name="PortalID"></param>
        ''' <param name="UserID"></param>
        ''' <param name="PostSubject"></param>
        ''' <param name="PostBody"></param>
        ''' <param name="ForumID"></param>
        ''' <param name="ParentPostID"></param>
        ''' <param name="PostID"></param>
        ''' <param name="IsPinned"></param>
        ''' <param name="IsClosed"></param>
        ''' <param name="Status"></param>
        ''' <param name="lstAttachmentFileIDs">A string of comma separated integers(DNN File ID's). It is assumed that all attachments (and local post images) already exist within the DNN file system.</param>
        ''' <param name="RemoteAddress"></param>
        ''' <param name="PollID"></param>
        ''' <param name="IsQuote"></param>
        ''' <param name="Provider"></param>
        ''' <param name="ThreadID"></param>
        ''' <returns>A message indicating what happend, ie. if the post was successfull or why it failed.</returns>
        ''' <remarks>Internal and external methods for posting call this.</remarks>
        Private Function PostingValidation(ByVal TabID As Integer, ByVal ModuleID As Integer, ByVal PortalID As Integer, ByVal UserID As Integer, ByVal PostSubject As String, ByVal PostBody As String, ByVal ForumID As Integer, ByVal ParentPostID As Integer, ByVal PostID As Integer, ByVal IsPinned As Boolean, ByVal IsClosed As Boolean, ByVal ReplyNotify As Boolean, ByVal Status As Forum.ThreadStatus, ByVal lstAttachmentFileIDs As String, ByVal RemoteAddress As String, ByVal PollID As Integer, ByVal IsQuote As Boolean, ByVal Provider As String, ByVal ThreadID As Integer, ByVal Terms As List(Of DotNetNuke.Entities.Content.Taxonomy.Term)) As PostMessage
            Dim cntForum As New ForumController
            Dim objForum As New ForumInfo
            Dim cntForumUser As New ForumUserController
            Dim objForumUser As ForumUserInfo

            objConfig = Forum.Configuration.GetForumConfig(ModuleID)
            objForum = cntForum.GetForumItemCache(ForumID)
            objForumUser = cntForumUser.GetForumUser(UserID, False, ModuleID, PortalID)

            Dim objModSecurity As New Forum.ModuleSecurity(objConfig.ModuleID, TabID, objForum.ForumID, objForumUser.UserID)
            Dim IsModerated As Boolean = False

            If (objForum.IsModerated) And Not (objForumUser.IsTrusted Or objModSecurity.IsUnmoderated) Then
                IsModerated = True
            End If

            If PostSubject.Trim() = String.Empty Then
                Return PostMessage.PostInvalidSubject
            End If

            If PostBody.Trim() = String.Empty Then
                Return PostMessage.PostInvalidBody
            End If

            If objForumUser.IsBanned Then
                Return PostMessage.UserBanned
            End If

            If objForum Is Nothing Then
                Return PostMessage.ForumDoesntExist
            Else
                If objForum.ContainsChildForums Then
                    Return PostMessage.ForumIsParent
                End If

                If Not objForum.IsActive Then
                    Return PostMessage.ForumClosed
                End If

                If Not objForum.PublicView Then
                    If Not objModSecurity.IsAllowedToViewPrivateForum Then
                        Return PostMessage.UserCannotViewForum
                    End If
                End If

                If lstAttachmentFileIDs <> String.Empty And Not objConfig.EnableAttachment Then
                    Return PostMessage.ForumNoAttachments
                Else
                    If lstAttachmentFileIDs <> String.Empty And Not objModSecurity.CanAddAttachments Then
                        Return PostMessage.UserAttachmentPerms
                    End If
                End If

                Dim objAction As PostAction

                If PostID > 0 Then
                    objAction = PostAction.Edit
                Else
                    If ParentPostID > 0 Then
                        If IsQuote Then
                            objAction = PostAction.Quote
                        Else
                            objAction = PostAction.Reply
                        End If
                    Else
                        objAction = PostAction.New
                    End If
                End If

                If Not objForum.PublicPosting Then
                    Select Case objAction
                        Case PostAction.New
                            If Not (objModSecurity.IsAllowedToStartRestrictedThread) Then
                                Return PostMessage.UserCannotStartThread
                            End If
                        Case Else
                            If Not (objModSecurity.IsAllowedToPostRestrictedReply) Then
                                Return PostMessage.UserCannotPostReply
                            End If
                    End Select
                End If

                If objAction = PostAction.Edit Then
                    Dim objPost As New PostInfo
                    Dim cntPost As New PostController
                    Dim CanAlwaysEdit As Boolean = False

                    objPost = cntPost.GetPostInfo(PostID, PortalID)

                    If objModSecurity.IsForumAdmin = True Then CanAlwaysEdit = True
                    If objModSecurity.IsForumModerator = True Then CanAlwaysEdit = True
                    If objModSecurity.IsGlobalModerator = True Then CanAlwaysEdit = True
                    If objModSecurity.IsModerator = True Then CanAlwaysEdit = True

                    If CanAlwaysEdit = False Then
                        ' let's first see if the user is the poster
                        If objPost.UserID = UserID Then
                            If objConfig.PostEditWindow > 0 Then
                                If objPost.CreatedDate.AddMinutes(CDbl(objConfig.PostEditWindow)) < DateTime.Now Then
                                    Return PostMessage.PostEditExpired
                                End If
                            End If
                        Else
                            Return PostMessage.UserCannotEditPost
                        End If
                    End If
                End If

                Dim FinalSubject As String = ProcessPostSubject(PostSubject, objConfig, PortalID)
                Dim FinalBody As String = ProcessPostBody(PostBody, objConfig, PortalID, objAction, objForumUser.UserID)
                Dim NewPostID As Integer = -1

                NewPostID = PostToDatabase(TabID, ModuleID, objConfig, PortalID, objForumUser, FinalSubject, FinalBody, objForum, ParentPostID, PostID, IsPinned, IsClosed, ReplyNotify, Status, lstAttachmentFileIDs, RemoteAddress, PollID, ThreadID, objAction, IsModerated, Terms)

                If NewPostID > 0 Then
                    If IsModerated Then
                        Return PostMessage.PostModerated
                    Else
                        Return PostMessage.PostApproved
                    End If
                End If

                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' This submits the post to the database.  It also calls email notification 
        ''' if enabled.  It takes moderation into consideration and fires off the 
        ''' necessary actions if needed. It also filters the post from here
        ''' for security reasons as well as bad words (if enabled), and calls all parsing (for image replacement, etc.).
        ''' </summary>
        ''' <remarks>All permissions and validation checks should be done prior to this method.</remarks>
        Private Function PostToDatabase(ByVal TabID As Integer, ByVal ModuleID As Integer, ByVal objConfig As Forum.Configuration, ByVal PortalID As Integer, ByVal objForumUser As ForumUserInfo, ByVal PostSubject As String, ByVal PostBody As String, ByVal objForum As ForumInfo, ByVal ParentPostID As Integer, ByVal PostID As Integer, ByVal IsPinned As Boolean, ByVal IsClosed As Boolean, ByVal ReplyNotify As Boolean, ByVal Status As Forum.ThreadStatus, ByVal lstAttachmentFileIDs As String, ByVal RemoteAddress As String, ByVal PollID As Integer, ByVal ThreadID As Integer, ByVal objAction As PostAction, ByVal IsModerated As Boolean, ByVal Terms As List(Of DotNetNuke.Entities.Content.Taxonomy.Term)) As Integer
            Dim objSecurity As New PortalSecurity
            Dim newPostID As Integer
            Dim objNewPost As New PostInfo
            Dim _PinnedDate As DateTime = DateTime.Today

            RemoteAddress = objSecurity.InputFilter(RemoteAddress, PortalSecurity.FilterFlag.NoMarkup)
            objConfig = objConfig

            Select Case objConfig.EnableAttachment
                Case True
                    lstAttachmentFileIDs = lstAttachmentFileIDs
                Case Else
                    lstAttachmentFileIDs = String.Empty
            End Select

            'Here we handle ParseInfo
            Dim ParsingType As Integer

            'If ProcessParseInfo = False Then
            '	ParsingType = 0
            'Else
            'Look for Attachments
            If objAction = PostAction.New Then
                ParsingType = 0
            Else
                If objConfig.EnableAttachment Then
                    If lstAttachmentFileIDs <> String.Empty Then
                        Dim cntAttachment As New AttachmentController
                        ParsingType = CalculateParseInfo(PostBody, cntAttachment.GetAllByPostID(PostID), objConfig)
                    End If
                End If
            End If
            'End If

            Dim ctlPost As New PostController
            Dim _emailType As ForumEmailType

            ' Add/Edit post
            Select Case objAction
                Case PostAction.[New]
                    ' we are clearing out attachments (empty string) as this method is now legacy
                    newPostID = ctlPost.PostAdd(0, objForum.ForumID, objForumUser.UserID, RemoteAddress, PostSubject, PostBody, IsPinned, _PinnedDate, IsClosed, PortalID, PollID, IsModerated, objForum.GroupID, objForum.ParentID, ParsingType)
                    ' If thread status is enabled and there is an edit on the first post in a thread, make sure we set the thread status
                    ' Remeber that the threadID is equal to the postid of the first post in a thread.
                    If objConfig.EnableThreadStatus And objForum.EnableForumsThreadStatus Then
                        If Status > 0 Then
                            Dim ctlThread As New ThreadController
                            ctlThread.ChangeThreadStatus(newPostID, objForumUser.UserID, Status, 0, -1, PortalID)
                        End If
                        ' even if thread status is off, user may be allowed to add a poll which means we need to set the status to "Poll"
                    ElseIf objForum.AllowPolls And PollID > 0 Then
                        Dim ctlThread As New ThreadController
                        ctlThread.ChangeThreadStatus(newPostID, objForumUser.UserID, Status, 0, -1, PortalID)
                    End If

                    ' Handle Content Item Creation
                    If (ThreadID = -1) AndAlso (newPostID > 0) Then
                        Dim cntThread As New ThreadController
                        Dim objThread As ThreadInfo = cntThread.GetThread(newPostID)

                        objThread.ModuleID = ModuleID
                        objThread.TabID = TabID
                        objThread.SitemapInclude = objForum.EnableSitemap
                        objThread.Terms.Clear()
                        objThread.Terms.AddRange(Terms)

                        Dim cntContent As New Content
                        cntContent.CreateContentItem(objThread, TabID)

                        ThreadID = objThread.ThreadID
                    End If

                    Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID, objForum.ForumID, objForum.GroupID, objConfig.ModuleID, objForum.ParentID)

                    _emailType = ForumEmailType.UserNewThread
                Case PostAction.Edit
                    newPostID = PostID
                    ' we are clearing out attachments (empty string) as this method is now legacy
                    ctlPost.PostUpdate(ThreadID, newPostID, PostSubject, PostBody, IsPinned, _PinnedDate, IsClosed, objForumUser.UserID, PortalID, PollID, objForum.ParentID, ParsingType)
                    ' If thread status is enabled and there is an edit on the first post in a thread, make sure we set the thread status
                    If objConfig.EnableThreadStatus And ParentPostID = 0 Then
                        Dim ctlThread As New ThreadController
                        ctlThread.ChangeThreadStatus(ThreadID, objForumUser.UserID, Status, 0, -1, PortalID)
                        ' even if thread status is off, user may be allowed to add a poll which means we need to set the status to "Poll"
                    ElseIf objForum.AllowPolls And PollID > 0 And ParentPostID = 0 Then
                        Dim ctlThread As New ThreadController
                        ctlThread.ChangeThreadStatus(newPostID, objForumUser.UserID, Status, 0, -1, PortalID)
                    End If

                    Forum.Components.Utilities.Caching.UpdatePostCache(newPostID, ThreadID)

                    ' Handle Content Item (if postid = threadid)
                    If ThreadID = newPostID Then
                        Dim cntThread As New ThreadController
                        Dim objThread As ThreadInfo = cntThread.GetThread(ThreadID)

                        objThread.ModuleID = objConfig.ModuleID
                        objThread.TabID = TabID
                        objThread.SitemapInclude = objForum.EnableSitemap
                        objThread.Terms.Clear()
                        objThread.Terms.AddRange(Terms)

                        Dim cntContent As New Content
                        cntContent.UpdateContentItem(objThread, TabID)
                    End If

                    _emailType = ForumEmailType.UserPostEdited
                Case Else     ' Reply/Quote
                    ' we are clearing out attachments (empty string) as this method is now legacy
                    newPostID = ctlPost.PostAdd(ParentPostID, objForum.ForumID, objForumUser.UserID, RemoteAddress, PostSubject, PostBody, IsPinned, _PinnedDate, IsClosed, PortalID, PollID, IsModerated, objForum.GroupID, ParentPostID, ParsingType)
                    ' since it is a new post, we only need to update thread & forum
                    Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID, objForum.ForumID, objForum.GroupID, objConfig.ModuleID, objForum.ParentID)
                    _emailType = ForumEmailType.UserPostAdded
            End Select

            'If objForum.ParentID > 0 Then
            '	Forum.Components.Utilities.Caching.UpdateForumCache(objForum.ParentID, objForum.GroupID, objConfig.ModuleID, objForum.ParentID)
            'End If

            UserThreadsController.ResetUserThreadReadCache(objForumUser.UserID, ThreadID)
            DotNetNuke.Modules.Forum.Components.Utilities.Caching.UpdateUserCache(objForumUser.UserID, PortalID)

            ' Obtain a new instance of postinfo 
            Dim cntPost As New PostController()
            objNewPost = cntPost.GetPostInfo(newPostID, PortalID)

            If lstAttachmentFileIDs <> String.Empty Then
                HandleAttachments(objConfig, objForumUser.UserID, objAction, objNewPost)
            End If

            If ReplyNotify Then
                Dim cntTracking As New TrackingController()
                cntTracking.TrackingThreadCreateDelete(objForum.ForumID, ThreadID, objForumUser.UserID, ReplyNotify, ModuleID)
            End If

            HandleThreadStatus(objForum, objForumUser.UserID, Status, PollID, ParentPostID, objNewPost.PostID, objAction, PortalID)
            HandleNotifications(IsModerated, objConfig, objNewPost, _emailType, TabID, PortalID)

            Return newPostID
        End Function

        ''' <summary>
        ''' This method properly assigns attachments to a post, it is assumed the files are already uploaded to the DNN file system and that configuration is enabled. 
        ''' </summary>
        ''' <param name="objConfig"></param>
        ''' <param name="UserID"></param>
        ''' <param name="objAction"></param>
        ''' <param name="newPostInfo"></param>
        ''' <remarks></remarks>
        Private Sub HandleAttachments(ByVal objConfig As Forum.Configuration, ByVal UserID As Integer, ByVal objAction As Forum.PostAction, ByVal newPostInfo As PostInfo)
            ' We start by picking up any previously uploaded files, still not related to a postid
            Dim cntAttachment As New AttachmentController
            Dim lstAttachment As List(Of AttachmentInfo) = cntAttachment.GetAllByUserID(UserID)

            ' Do we need to check for existing items?
            If objAction = PostAction.Edit Then
                Dim oldAttachments As List(Of AttachmentInfo) = cntAttachment.GetAllByPostID(newPostInfo.PostID)
                If oldAttachments.Count > 0 Then
                    lstAttachment.AddRange(oldAttachments)
                End If
            End If

            If lstAttachment.Count > 0 Then
                ' There are attachments, add the postid and check for inline
                Dim DoResubmit As Boolean = False
                Dim Delete As String = String.Empty
                Dim strPostBody As String

                For Each objFile As AttachmentInfo In lstAttachment
                    ' Add the postID
                    objFile.PostID = newPostInfo.PostID

                    ' Any inline placements?
                    If newPostInfo.Body.ToLower.IndexOf("[attachment]" & objFile.LocalFileName.ToLower & "[/attachment]") >= 0 Then
                        objFile.Inline = True
                        'Check the width
                        If objFile.Width > objConfig.MaxPostImageWidth Then
                            Dim strMessage As String = CreateThumbnail(objFile, newPostInfo.PostID, objConfig)
                            If strMessage <> String.Empty Then
                                ' Something went wrong, We have to convert this to a regular attachment
                                objFile.Inline = False
                                strPostBody = newPostInfo.Body.Replace("[attachment]" & objFile.LocalFileName.ToLower & "[/attachment]", "")
                                DoResubmit = True
                            Else
                                ' There is file we need to delete, but file is currently locked, 
                                ' so we set it for deletion in the attachment object with PostID as -2
                                objFile.PostID = -2
                                cntAttachment.Update(objFile)
                            End If
                        Else
                            ' Update the attachment only if not resized!
                            cntAttachment.Update(objFile)
                        End If
                    Else
                        ' Update the attachment
                        cntAttachment.Update(objFile)
                    End If
                Next

            End If
        End Sub

        ''' <summary>
        ''' This method is used to change the status of a thread. 
        ''' </summary>
        ''' <param name="objForum"></param>
        ''' <param name="UserID"></param>
        ''' <param name="Status"></param>
        ''' <param name="PollID"></param>
        ''' <param name="ParentPostID"></param>
        ''' <param name="PostID"></param>
        ''' <param name="objAction"></param>
        ''' <remarks></remarks>
        Private Sub HandleThreadStatus(ByVal objForum As ForumInfo, ByVal UserID As Integer, ByVal Status As Forum.ThreadStatus, ByVal PollID As Integer, ByVal ParentPostID As Integer, ByVal PostID As Integer, ByVal objAction As Forum.PostAction, ByVal PortalID As Integer)
            Select Case objAction
                Case PostAction.New
                    ' If thread status is enabled and there is an edit on the first post in a thread, make sure we set the thread status
                    ' Remeber that the threadID is equal to the postid of the first post in a thread.
                    If objConfig.EnableThreadStatus And objForum.EnableForumsThreadStatus Then
                        If Status > 0 Then
                            Dim ctlThread As New ThreadController
                            ctlThread.ChangeThreadStatus(PostID, UserID, Status, 0, -1, objConfig.CurrentPortalSettings.PortalId)
                        End If
                        ' even if thread status is off, user may be allowed to add a poll which means we need to set the status to "Poll"
                    ElseIf objForum.AllowPolls And PollID > 0 Then
                        Dim ctlThread As New ThreadController
                        ctlThread.ChangeThreadStatus(PostID, UserID, Convert.ToInt32(ThreadStatus.Poll), 0, -1, PortalID)
                    End If
                Case PostAction.Edit
                    ' If thread status is enabled and there is an edit on the first post in a thread, make sure we set the thread status
                    If objConfig.EnableThreadStatus And ParentPostID = -1 Then
                        If Status > 0 Then
                            Dim ctlThread As New ThreadController
                            'NOTE: CP - COMEBACK: It may be possible for a thread status to be edited on the original post, for which we should send an update if it is a moderator.
                            ctlThread.ChangeThreadStatus(PostID, UserID, Status, 0, -1, PortalID)
                        End If
                        ' even if thread status is off, user may be allowed to add a poll which means we need to set the status to "Poll"
                    ElseIf objForum.AllowPolls And PollID > 0 And ParentPostID = -1 Then
                        Dim ctlThread As New ThreadController
                        ctlThread.ChangeThreadStatus(PostID, UserID, Convert.ToInt32(ThreadStatus.Poll), 0, -1, PortalID)
                    End If
            End Select
        End Sub

        ''' <summary>
        ''' This method is called by PostToDatabase AFTER a post is submitted so that any notifications can be sent out based on the post action and results that just occurred. 
        ''' </summary>
        ''' <param name="IsModerated"></param>
        ''' <param name="objConfig"></param>
        ''' <param name="newPostInfo"></param>
        ''' <param name="emailType"></param>
        ''' <param name="TabID"></param>
        ''' <param name="PortalID"></param>
        ''' <remarks></remarks>
        Private Sub HandleNotifications(ByVal IsModerated As Boolean, ByVal objConfig As Forum.Configuration, ByVal newPostInfo As PostInfo, ByVal emailType As Forum.ForumEmailType, ByVal TabID As Integer, ByVal PortalID As Integer)
            ' Send notification email & update forum post added info
            Dim _mailURL As String
            Dim ProfileUrl As String = Utilities.Links.UCP_UserLinks(TabID, objConfig.ModuleID, UserAjaxControl.Tracking, objConfig.CurrentPortalSettings)

            ' If the post is to be moderated
            If IsModerated Then
                ' we set this here as it requires a seperate mail call
                emailType = ForumEmailType.ModeratorPostToModerate

                ' This will now take moderator directly to post to moderate from email.
                _mailURL = Utilities.Links.ContainerPostToModerateLink(TabID, newPostInfo.ForumID, newPostInfo.ModuleId)

                If objConfig.MailNotification Then
                    Utilities.ForumUtils.SendForumMail(newPostInfo.PostID, _mailURL, emailType, "Moderated Post", objConfig, ProfileUrl, PortalID)
                    ' possibly email user that their post is in queue - removed functionality from previous versions
                End If
            Else
                ' This is for a non moderated post (posted by trusted user, or the forum is not moderated)
                ' We have to determine which tab we want to view becuase of group feature formerly used on dnn.com (for now leaving as tab posted from because of possible perms issues)
                _mailURL = Utilities.Links.ContainerViewPostLink(TabID, newPostInfo.ForumID, newPostInfo.PostID)

                If objConfig.MailNotification Then
                    If emailType = ForumEmailType.UserPostEdited Then
                        If objConfig.EnableEditEmails Then
                            Utilities.ForumUtils.SendForumMail(newPostInfo.PostID, _mailURL, emailType, "Unmoderated Post", objConfig, ProfileUrl, PortalID)
                        End If
                    Else
                        Utilities.ForumUtils.SendForumMail(newPostInfo.PostID, _mailURL, emailType, "Unmoderated Post", objConfig, ProfileUrl, PortalID)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' This generates a thumbnail from the destination image. Since we are
        ''' changing the size, we will need a new fileid from DNN...
        ''' </summary>
        ''' <param name="objOldFile"></param>
        ''' <param name="PostID"></param>
        ''' <remarks></remarks>
        Private Function CreateThumbnail(ByVal objOldFile As AttachmentInfo, ByVal PostID As Integer, ByVal objConfig As Forum.Configuration) As String
            Dim strMessage As String = String.Empty
            Try

                Dim BaseFolder As String = objConfig.AttachmentPath
                If BaseFolder.EndsWith("/") = False Then BaseFolder += "/"
                Dim ParentFolderName As String = objConfig.CurrentPortalSettings.HomeDirectoryMapPath
                ParentFolderName += BaseFolder
                ParentFolderName = ParentFolderName.Replace("/", "\")
                If ParentFolderName.EndsWith("\") = False Then ParentFolderName += "\"

                Dim srcImage As New System.Drawing.Bitmap(ParentFolderName & objOldFile.FileName)

                'Find out the height to width ratio
                Dim dblRatio As Double = CDbl(srcImage.Width / srcImage.Height)
                'apply height to width ratio to thumbnail
                Dim ImageHeight As Integer = CInt(objConfig.MaxPostImageWidth / dblRatio)
                Dim newSize As New System.Drawing.Size(objConfig.MaxPostImageWidth, ImageHeight)

                Dim myPixelFormat As Drawing.Imaging.PixelFormat
                myPixelFormat = srcImage.PixelFormat

                Dim myImageFormat As Drawing.Imaging.ImageFormat
                myImageFormat = srcImage.RawFormat

                Dim newImg As New System.Drawing.Bitmap(newSize.Width, newSize.Height)
                Dim recDest As New Drawing.Rectangle(0, 0, newSize.Width, newSize.Height)
                Dim mGraphics As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(newImg)

                mGraphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality
                mGraphics.CompositingQuality = Drawing.Drawing2D.CompositingQuality.HighQuality
                mGraphics.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                mGraphics.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality

                mGraphics.DrawImage(srcImage, recDest)
                mGraphics.Dispose()
                srcImage.Dispose()

                'Generate new GUID and save!
                Dim destFileName As String = Guid.NewGuid().ToString().Replace("-", "") + "." + objOldFile.Extension
                Dim parentFolderInfo As IFolderInfo = FileUtilityClass.GetFolder(ParentFolderName, GetPortalSettings.PortalId)
                Using stream As MemoryStream = New MemoryStream(ConvertImageToByteArray(newImg, myImageFormat))
                    FileManager.Instance.AddFile(parentFolderInfo, destFileName, stream)
                End Using
                newImg.Dispose()

                'Get the new FileID
                Dim myFileID As Integer = 0
                Dim fileList As ArrayList = Common.Globals.GetFileList(objConfig.CurrentPortalSettings.PortalId, objOldFile.Extension, False, BaseFolder, False)
                For Each objFile As FileItem In fileList
                    If objFile.Text = destFileName Then
                        myFileID = CInt(objFile.Value)
                    End If
                Next

                'Save the resized Attachment
                If myFileID > 0 Then
                    Dim objNewFile As New AttachmentInfo
                    With objNewFile
                        .FileID = myFileID
                        .PostID = PostID
                        .LocalFileName = objOldFile.LocalFileName
                        .Inline = True
                        .UserID = objOldFile.UserID
                    End With
                    Dim cntAttachment As New AttachmentController
                    cntAttachment.Update(objNewFile)

                Else
                    'Something went wrong, we'll have to abort
                    FileUtilityClass.DeleteFile(ParentFolderName, destFileName, GetPortalSettings.PortalId)
                End If

            Catch ex As Exception
                DotNetNuke.Services.Exceptions.LogException(ex)
                strMessage = ex.Message
            End Try

            Return strMessage
        End Function

        ''' <summary>
        '''Converts an image to a byte array
        ''' </summary>
        '''<param name="imgConvert"></param>
        ''' <param name="imgFormat"></param>
        ''' <remarks></remarks>
        Private Function ConvertImageToByteArray(ByVal imgConvert As System.Drawing.Image, ByVal imgFormat As System.Drawing.Imaging.ImageFormat) As Byte()
            Dim Ret As Byte() = Nothing

            Try

                Using ms As New System.IO.MemoryStream()
                    imgConvert.Save(ms, imgFormat)
                    Ret = ms.ToArray()
                End Using
            Catch ex As Exception
                DotNetNuke.Services.Exceptions.LogException(ex)
            End Try

            Return Ret
        End Function

        ''' <summary>
        ''' Generates a list of Attachments for Preview
        ''' </summary>
        ''' <remarks></remarks>
        Private Function GetPreviewAttachments(ByVal strPreview As String, ByVal objAction As PostAction, ByVal UserID As Integer) As List(Of AttachmentInfo)
            'Get the AttachmentList
            Dim cntAttachment As New AttachmentController
            Dim lstAttachments As List(Of AttachmentInfo) = cntAttachment.GetAllByUserID(UserID)

            If objAction <> PostAction.New Then
                'Dim PostID As Integer = Int32.Parse(Request.QueryString("postid"))
                'Dim lstPostAttachments As List(Of AttachmentInfo) = cntAttachment.GetAllByPostID(PostID)
                'If lstPostAttachments.Count > 0 Then
                '	lstAttachments.AddRange(lstPostAttachments)
                'End If
            End If

            ' As this is preview, we need to modify the inline statements
            If lstAttachments.Count > 0 Then
                Dim RegOptions As RegexOptions = RegexOptions.IgnoreCase Or RegexOptions.Multiline Or RegexOptions.Singleline
                For Each objFile As AttachmentInfo In lstAttachments
                    Dim regInline As New Regex("\[attachment\]" & objFile.LocalFileName & "\[/attachment\]", RegOptions)
                    If regInline.IsMatch(strPreview) Then
                        objFile.Inline = True
                    End If
                Next
            End If

            Return lstAttachments
        End Function

        ''' <summary>
        ''' This function will test any <a href=""></a> instances of the postbody and do the following:
        ''' 1) Add Target="_Blank" and NoFollow to all external links
        ''' 2) Check the lenght of the link name and reduce it if necessary
        ''' </summary>
        ''' <param name="mMatch"></param>
        ''' <remarks>Added by Skeel</remarks>
        Public Shared Function ReplaceUrls(ByVal mMatch As Match) As String
            Dim strUrl As String = String.Empty

            'Get the URL
            Dim regExp As New Regex("href=[\""]?([^""\s]*)[\""]?")
            Dim urlMatch As Match

            urlMatch = regExp.Match(mMatch.Value)

            If urlMatch.Success = True Then
                'Now for the length of the link name
                Dim strtmp As String = UrlUtilityClass.UrlShortener(mMatch.Value)
                If strtmp <> mMatch.Value Then
                    strUrl = strtmp
                Else
                    strUrl = mMatch.Value
                End If
            Else
                'Something's wrong ...return original html string
                strUrl = mMatch.Value
            End If

            Return strUrl
        End Function

        ''' <summary>
        ''' This function will test the width of an image and set a Width and Height
        ''' parameter equal to objConfig.MaxPostImageWidth if the image is larger. The image proportions will be kept intact.
        ''' </summary>
        ''' <param name="mMatch">The image to.</param>
        ''' <remarks>Added by Skeel</remarks>
        Private Function ReplaceImageUrl(ByVal mMatch As Match) As String
            Dim strImage As String = String.Empty

            Try
                Dim GetImage As Boolean = True
                'Does the image have a width set already?
                Dim regExp As New Regex("(?i)(\s*width=)[\""]?([^""\s]*)[\""]?")
                Dim ImageMatch As Match

                ImageMatch = regExp.Match(mMatch.Value)
                If ImageMatch.Success = True Then
                    Dim w As Integer = CInt(ImageMatch.Groups(2).Value)
                    If w <= objConfig.MaxPostImageWidth Then
                        'image okay, return original html string
                        strImage = mMatch.Value
                        GetImage = False
                    End If
                End If

                If GetImage = True Then
                    'Extract the image source
                    regExp = New Regex("src=[\""]?([^""\s]*)[\""]?")
                    ImageMatch = regExp.Match(mMatch.Value)
                    Dim imageUrl As String = ImageMatch.Groups(1).Value

                    'Get the image from URI
                    Dim request As System.Net.HttpWebRequest = TryCast(System.Net.WebRequest.Create(imageUrl), System.Net.HttpWebRequest)
                    Dim response As System.Net.HttpWebResponse = TryCast(request.GetResponse(), System.Net.HttpWebResponse)
                    Dim stream As System.IO.Stream = response.GetResponseStream()
                    Dim ms As New System.IO.MemoryStream()
                    Dim bw As New System.IO.BinaryWriter(ms)
                    Dim br As New System.IO.BinaryReader(stream)
                    bw.Write(br.ReadBytes(CInt(response.ContentLength)))
                    ms.Position = 0
                    Dim srcImage As System.Drawing.Bitmap = TryCast(System.Drawing.Bitmap.FromStream(ms), System.Drawing.Bitmap)

                    'Clean up, we got the Image!
                    br.Close()
                    bw.Close()
                    ms.Close()
                    stream.Close()
                    response.Close()

                    'Check the width
                    If srcImage.Width > objConfig.MaxPostImageWidth Then

                        'Calculate new image size
                        Dim w As Integer = srcImage.Width
                        Dim h As Integer = srcImage.Height
                        Dim f As Decimal = CDec(objConfig.MaxPostImageWidth / w)
                        h = CInt(Math.Ceiling(h * f))
                        strImage = "<img src=""" & imageUrl & """ height=""" & CStr(h) & """ width=""" & CStr(objConfig.MaxPostImageWidth) & """ border=""0"" />"
                    Else
                        'Width is smaller than allowed, return the original html string
                        strImage = mMatch.Value
                    End If
                End If
            Catch ex As Exception
                'Something's wrong with the image...return original html string
                strImage = mMatch.Value
            End Try

            Return strImage
        End Function

        ''' <summary>
        ''' This function will calculate the Post ParserInfo value as a som of any 
        ''' Enum PostParserInfo, that apply to the specific Post, in relation to Forum Configuration
        ''' </summary>
        ''' <param name="PostBody"></param>
        ''' <param name="PostAttachments"></param>
        ''' <param name="objConfig"></param>
        ''' <remarks>Added by Skeel</remarks>
        Private Function CalculateParseInfo(ByVal PostBody As String, ByVal PostAttachments As List(Of AttachmentInfo), ByVal objConfig As Forum.Configuration) As Integer
            'Here we handle ParseInfo
            Dim ParseInfo As Integer = 0

            'We always look for BBCode
            If PostBody.IndexOf("[quote") >= 0 Or PostBody.IndexOf("[code") >= 0 Then
                ParseInfo += PostParserInfo.BBCode
            End If

            'Look for Attachments if enabled
            If objConfig.EnableAttachment = True Then
                If PostAttachments.Count > 0 Then

                    Dim HasAttachment As Boolean = False
                    Dim HasInlineAttachment As Boolean = False

                    For Each objFile As AttachmentInfo In PostAttachments
                        If objFile.Inline = True Then
                            HasInlineAttachment = True
                        Else
                            HasAttachment = True
                        End If
                    Next

                    If HasAttachment = True Then
                        ParseInfo += PostParserInfo.File
                    End If

                    If HasInlineAttachment = True Then
                        ParseInfo += PostParserInfo.Inline
                    End If

                End If
            End If

            'Return the sum
            Return ParseInfo
        End Function

#End Region

    End Class

End Namespace
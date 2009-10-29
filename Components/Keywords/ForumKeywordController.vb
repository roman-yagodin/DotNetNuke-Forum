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

#Region "ForumKeywordController"

    ''' <summary>
    ''' Connects the business layer to the data layer for forum Keywords (used for template parsing).
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    ''' 	[cpaterra]	8/27/2006	Created
    ''' </history>
    Public Class ForumKeywordController

        ''' <summary>
        ''' Retrieves a collection of Keywords from the data store based on content type.
        ''' </summary>
        ''' <param name="ContentTypeID">The type of content to retrieve keywords for.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetKeywordsByType(ByVal ContentTypeID As Integer) As ArrayList
            Return CBO.FillCollection(DataProvider.Instance().GetKeywordsByType(ContentTypeID), GetType(ForumKeywordInfo))
        End Function

    End Class

#End Region

End Namespace
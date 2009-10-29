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
    ''' All properties associated with the Forum_Polls_Answers table. 
    ''' </summary>
    ''' <remarks></remarks>
    Public Class AnswerInfo

#Region "Private Members"

        Private _AnswerID As Integer
        Private _PollID As Integer
        Private _Answer As String
        Private _SortOrder As Integer

        Private _AnswerCount As Integer

#End Region

#Region "Constructors"

        ''' <summary>
        ''' Instantiates the object. 
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
        End Sub

#End Region

#Region "Public Properties"

        ''' <summary>
        ''' The primary key value for the Answers table.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property AnswerID() As Integer
            Get
                Return _AnswerID
            End Get
            Set(ByVal Value As Integer)
                _AnswerID = Value
            End Set
        End Property

        ''' <summary>
        ''' The foreign key value for the Polls table.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property PollID() As Integer
            Get
                Return _PollID
            End Get
            Set(ByVal Value As Integer)
                _PollID = Value
            End Set
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property Answer() As String
            Get
                Return _Answer
            End Get
            Set(ByVal Value As String)
                _Answer = Value
            End Set
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property SortOrder() As Integer
            Get
                Return _SortOrder
            End Get
            Set(ByVal Value As Integer)
                _SortOrder = Value
            End Set
        End Property

        Public Property AnswerCount() As Integer
            Get
                Return _AnswerCount
            End Get
            Set(ByVal Value As Integer)
                _AnswerCount = Value
            End Set
        End Property

#End Region

    End Class

End Namespace
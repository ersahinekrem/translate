'Translate Manager V1.0.0.3
'Author Gozeer Digital
'http://gozeer.com
'ekrem@gozeer.com

Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO

Namespace gozeer.translate
    Public Class TranslateManager
        Inherits System.IO.Stream
        Private _baseStream As System.IO.Stream
        Private _position As Long
        Private _html As String = ""

        Private SelectedLanguage As String
        Public Sub New(stream As System.IO.Stream, SelectedLanguage As String)
            Me.SelectedLanguage = SelectedLanguage
            _baseStream = stream
        End Sub

        Public Sub New(stream As System.IO.Stream)
            Me.SelectedLanguage = System.Web.HttpContext.Current.Request.RequestContext.RouteData.Values("lang")
            _baseStream = stream
        End Sub

        Public Overrides ReadOnly Property CanRead() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property Length() As Long
            Get
                Return 0
            End Get
        End Property

        Public Overrides Property Position() As Long
            Get
                Return _position
            End Get
            Set(value As Long)
                _position = value
            End Set
        End Property

        Public Overrides Sub Write(buffer As Byte(), offset As Integer, count As Integer)
            _html = Encoding.UTF8.GetString(buffer, offset, count)
            _html = Translate(_html)
            buffer = Encoding.UTF8.GetBytes(_html)
            _baseStream.Write(buffer, 0, buffer.Length)
            System.GC.SuppressFinalize(_html)
            System.GC.SuppressFinalize(buffer)
            _baseStream.Close()
            _baseStream.Dispose()
        End Sub

        Private Function Translate(_html As String) As String
            Dim SelectedLanguage As String = System.Web.HttpContext.Current.Request.RequestContext.RouteData.Values("lang")
            Dim LanguageFiles As String = System.Web.Hosting.HostingEnvironment.MapPath(String.Format("~/languages/{0}", SelectedLanguage))

            Dim LngKeys As New System.Collections.Generic.Dictionary(Of String, String)

            If IO.Directory.Exists(LanguageFiles) Then
                For Each i In IO.Directory.GetFiles(LanguageFiles)
                    Dim xmlFile As New System.Xml.XmlDocument
                    xmlFile.Load(i)

                    For Each key As System.Xml.XmlNode In xmlFile.GetElementsByTagName("keys").Item(0).ChildNodes
                        If Not LngKeys.ContainsKey(key.Name) Then
                            LngKeys.Add(key.Name, key.InnerText)
                        End If
                    Next
                Next
            Else
                System.IO.Directory.CreateDirectory(LanguageFiles)
            End If

            Dim findWidgets2 = New Regex("\$translate\((.*?)\);", RegexOptions.Singleline Or RegexOptions.IgnoreCase Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.Multiline)

            For Each Matc As Match In findWidgets2.Matches(_html)
                If Matc.Success Then

                    Dim js As System.Collections.Generic.Dictionary(Of String, String)
                    Dim seri As New System.Web.Script.Serialization.JavaScriptSerializer
                    Try
                        js = seri.Deserialize(Of System.Collections.Generic.Dictionary(Of String, String))("{" & Matc.Result("$1").ToString & "}")


                        If js.ContainsKey("key") Then
                            If LngKeys.ContainsKey(js("key")) Then
                                If js.ContainsKey("parameters") AndAlso js("parameters").Length > 0 Then
                                    Dim p = js("parameters").Split(",")
                                    _html = findWidgets2.Replace(_html, String.Format(LngKeys(js("key")), p), 1)
                                Else
                                    _html = findWidgets2.Replace(_html, LngKeys(js("key")), 1)
                                End If
                            Else
                                If Not IO.File.Exists(System.Web.Hosting.HostingEnvironment.MapPath("~/languages/" & SelectedLanguage & "/_exclude.xml")) Then

                                    My.Computer.FileSystem.WriteAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/languages/" & SelectedLanguage & "/_exclude.xml"), "<?xml version=""1.0"" encoding=""utf-8""?><keys></keys>", False)

                                End If
                                Dim xml As New System.Xml.XmlDocument
                                xml.Load(System.Web.Hosting.HostingEnvironment.MapPath("~/languages/" & SelectedLanguage & "/_exclude.xml"))

                                Dim elem As System.Xml.XmlElement = xml.CreateElement(js("key"))
                                elem.InnerXml = js("default")
                                xml.GetElementsByTagName("keys").Item(0).AppendChild(elem)
                                xml.Save(System.Web.Hosting.HostingEnvironment.MapPath("~/languages/" & SelectedLanguage & "/_exclude.xml"))
                                If js.ContainsKey("parameters") AndAlso js("parameters").Length > 0 Then
                                    Dim p = js("parameters").Split(",")
                                    _html = findWidgets2.Replace(_html, String.Format(js("default"), p), 1)
                                Else
                                    _html = findWidgets2.Replace(_html, js("default"), 1)
                                End If
                            End If
                        Else
                            _html = findWidgets2.Replace(_html, "Translate Error : Translation Key Not Found. Please insert key", 1)
                        End If
                    Catch ex As Exception
                        _html = findWidgets2.Replace(_html, "Translate Error :" & Matc.Result("$1") & ex.Message, 1)
                    End Try

                End If
            Next

            Return _html
        End Function

        Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
            Return _baseStream.Read(buffer, offset, count)
        End Function

        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Return _baseStream.Seek(offset, origin)
        End Function

        Public Overrides Sub SetLength(value As Long)
            _baseStream.SetLength(value)
        End Sub

        Public Overrides Sub Flush()
            Dim output As String = _html
            _baseStream.Flush()
        End Sub
    End Class

    <Global.Microsoft.VisualBasic.HideModuleNameAttribute(), _
  Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), _
  Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()> _
    Public Module Helpers
        Public Function CreateKey(Key As String, DefaultValue As String, Optional Parameters() As String = Nothing) As String
            If Parameters IsNot Nothing Then
                Dim sb As New System.Text.StringBuilder
                For Each P In Parameters
                    If sb.ToString = "" Then
                        sb.Append(P)
                    Else
                        sb.Append("," & P)
                    End If

                Next
                Return String.Format("$translate(key:'{0}',default:'{1}',parameters:'{2}');", Key, DefaultValue, sb.ToString)
            Else
                Return String.Format("$translate(key:'{0}',default:'{1}');", Key, DefaultValue)
            End If

        End Function
    End Module
End Namespace

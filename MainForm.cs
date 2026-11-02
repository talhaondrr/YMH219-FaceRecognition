using OpenCvSharp;
namespace Lab;
public sealed class MainForm:Form
{
 FaceService? service;
 VideoCapture? camera;
 Mat? frame;
 readonly System.Windows.Forms.Timer timer=new(){Interval=100};
 readonly PictureBox picture=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(25,32,45)};
 readonly TextBox label=new(){Width=130,PlaceholderText="Gönüllü etiketi"};
 readonly Label status=Ui.Label("Hazır • Yalnızca gönüllü yerel örnekler kullanın.");
 readonly NumericUpDown cameraIndex=new(){Minimum=0,Maximum=10,Width=45};
 public MainForm()
 {
  Ui.Setup(this,"OpenCV • Yüz Algılama ve Tanıma");var bar=Ui.Bar();bar.Controls.AddRange([Ui.Label("Kamera"),cameraIndex,Ui.Button("Kamera başlat",(_,_)=>Guard(StartCamera)),Ui.Button("Durdur",(_,_)=>StopCamera()),Ui.Button("Görüntü seç",(_,_)=>Guard(Choose)),label,Ui.Button("Örneği etikete ekle",(_,_)=>Guard(Add))]);status.Dock=DockStyle.Bottom;Controls.Add(picture);Controls.Add(status);Controls.Add(bar);
  timer.Tick+=(_,_)=>Guard(()=>{var next=new Mat();if(camera is null||!camera.Read(next)||next.Empty()){next.Dispose();StopCamera();throw new IOException("Kameradan görüntü alınamadı.");}SetFrame(next);});
  Shown+=(_,_)=>Guard(()=>{service=new FaceService();status.Text="Model hazır. Fotoğraf seçin veya kamerayı başlatın. Eğitim için en az 2 farklı etiket kullanın.";});
 }
 void Guard(Action action){try{action();}catch(Exception e){status.Text="Hata: "+e.Message;}}
 void StartCamera(){StopCamera();camera=new VideoCapture((int)cameraIndex.Value);if(!camera.IsOpened()){StopCamera();throw new IOException("Kamera açılamadı. Kamera bağlantısını ve Windows izinlerini kontrol edin.");}timer.Start();}
 void StopCamera(){timer.Stop();camera?.Dispose();camera=null;status.Text="Kamera durduruldu.";}
 void Choose(){using var dialog=new OpenFileDialog{Filter="Görüntüler|*.png;*.jpg;*.jpeg;*.bmp"};if(dialog.ShowDialog()!=DialogResult.OK)return;StopCamera();var next=Cv2.ImRead(dialog.FileName);if(next.Empty()){next.Dispose();throw new IOException("Görüntü okunamadı.");}SetFrame(next);}
 void SetFrame(Mat value){frame?.Dispose();frame=value;Render();}
 void Render(){if(frame is null||service is null)return;using var output=frame.Clone();var faces=service.Detect(frame);var predictions=new List<string>();foreach(var face in faces){string prediction=service.Predict(frame,face);predictions.Add(prediction);Cv2.Rectangle(output,face,Scalar.LimeGreen,2);Cv2.PutText(output,prediction,new OpenCvSharp.Point(face.X,Math.Max(20,face.Y-8)),HersheyFonts.HersheySimplex,0.5,Scalar.Yellow,1);}using var stream=new MemoryStream(output.ToBytes(".bmp"));using var bitmap=new Bitmap(stream);var old=picture.Image;picture.Image=new Bitmap(bitmap);old?.Dispose();status.Text=$"Yüz: {faces.Length} • Etiket: {service.ClassCount} • Örnek: {service.SampleCount} • "+string.Join("; ",predictions);}
 void Add(){if(service is null)throw new InvalidOperationException("Model yüklenemedi.");if(frame is null)throw new InvalidOperationException("Önce bir görüntü açın.");string name=label.Text.Trim();if(name.Length==0)throw new InvalidOperationException("Etiket girin.");if(name.Any(c=>c>127))throw new InvalidOperationException("Görüntü etiketi için ASCII harf kullanın (Ornek_A gibi).");var faces=service.Detect(frame);if(faces.Length!=1)throw new InvalidOperationException("Örnek eklemek için görüntüde tam bir yüz bulunmalı.");service.Add(frame,faces[0],name);Render();}
 protected override void Dispose(bool disposing){if(disposing){timer.Dispose();camera?.Dispose();frame?.Dispose();service?.Dispose();picture.Image?.Dispose();}base.Dispose(disposing);}
}

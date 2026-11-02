using OpenCvSharp;
using OpenCvSharp.Face;
namespace Lab;
public sealed class FaceService:IDisposable
{
 readonly CascadeClassifier cascade;
 readonly LBPHFaceRecognizer recognizer=LBPHFaceRecognizer.Create(1,8,8,8,80);
 readonly List<Mat> samples=new();
 readonly List<int> labels=new();
 readonly Dictionary<string,int> names=new();
 public int SampleCount=>samples.Count;
 public int ClassCount=>names.Count;
 public FaceService(){string path=Path.Combine(AppContext.BaseDirectory,"Models","haarcascade_frontalface_default.xml");if(!File.Exists(path))throw new FileNotFoundException("Yüz algılama modeli eksik: "+path);cascade=new CascadeClassifier(path);}
 public Rect[] Detect(Mat image){using var gray=new Mat();Cv2.CvtColor(image,gray,ColorConversionCodes.BGR2GRAY);Cv2.EqualizeHist(gray,gray);return cascade.DetectMultiScale(gray,1.1,5,minSize:new OpenCvSharp.Size(60,60));}
 Mat Normalize(Mat image,Rect face){using var crop=new Mat(image,face);using var gray=new Mat();Cv2.CvtColor(crop,gray,ColorConversionCodes.BGR2GRAY);var output=new Mat();Cv2.Resize(gray,output,new OpenCvSharp.Size(160,160));Cv2.EqualizeHist(output,output);return output;}
 public void Add(Mat image,Rect face,string name){if(!names.TryGetValue(name,out int label)){label=names.Count;names.Add(name,label);}samples.Add(Normalize(image,face));labels.Add(label);recognizer.Train(samples,labels);}
 public string Predict(Mat image,Rect face){if(names.Count<2)return "En az 2 etiket ekleyin";using var normalized=Normalize(image,face);recognizer.Predict(normalized,out int label,out double distance);string name=label<0?"Bilinmiyor":names.First(x=>x.Value==label).Key;return $"{name} | uzaklik: {distance:F1}";}
 public void Dispose(){cascade.Dispose();recognizer.Dispose();foreach(var sample in samples)sample.Dispose();}
}

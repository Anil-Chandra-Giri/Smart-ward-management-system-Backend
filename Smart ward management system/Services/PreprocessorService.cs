using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Smart_ward_management_system.Services
{
    public class PreprocessorService
    {
        public string PreprocessImage(string filePath)
        {
            // Load image using EmguCV
            var img = new Image<Bgr, byte>(filePath);

            // Convert to grayscale
            var gray = img.Convert<Gray, byte>();

            // Apply Gaussian blur to reduce noise
            CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(3, 3), 0);

            // Increase contrast using histogram equalization
            CvInvoke.EqualizeHist(gray, gray);

            // Optional: Thresholding to make text clearer
            CvInvoke.Threshold(gray, gray, 0, 255, ThresholdType.Binary | ThresholdType.Otsu);

            // Optional: Deskew (rotate to correct angle)
            // You can add deskew logic here if needed

            // Save processed image temporarily
            var processedPath = Path.Combine(Path.GetTempPath(), "processed.png");
            gray.Save(processedPath);

            return processedPath;
        }
    }
}

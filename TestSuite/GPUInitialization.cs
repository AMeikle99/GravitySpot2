using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSuite
{
    class GPUInitialization
    {
        public static string maskedBodyCode = @"
            bool Contains(__global int*,int,int);
            #define SILHOUETTE 1

            __kernel void MaskImage(__global uint* imagePixels, __global float* colorDepthPoints, __global char* bodyIndexBytePtr, __global int* bodyIndexParticipantMap, __global int* bodyIndexesToShow, __global int* bodyIndexesCount, __global int* depthFrameSize, __global int* imageType)
            {
              int pixelIndex = get_global_id(0);
  
              float colorToDepthX = colorDepthPoints[(pixelIndex * 2)];
              float colorToDepthY = colorDepthPoints[((pixelIndex*2)+1)];
  
              if (!isinf(colorToDepthX) && !isinf(colorToDepthY))
              {
                int pixelWidth = depthFrameSize[0];
                int pixelHeight = depthFrameSize[1];
    
                int depthX = (int)colorToDepthX;
                int depthY = (int)colorToDepthY;
    
                if (depthX >= 0 && depthY >= 0 && depthX < pixelWidth && depthY < pixelHeight)
                {
                  int depthPixelIndex = (depthY * pixelWidth) + depthX;
                  int bodyIndex = (int)bodyIndexBytePtr[depthPixelIndex];
      
                  if(Contains(bodyIndexesToShow, bodyIndexesCount[0], bodyIndex))
                  {
                    int mappedIndex = bodyIndexParticipantMap[bodyIndex];
                    if (imageType[0] == SILHOUETTE && mappedIndex != -1)
                    {
                      imagePixels[pixelIndex] = BodyColor(mappedIndex);
                    }
                    return;
                  }
                }
              }
  
              imagePixels[pixelIndex] = 0;  
            }

            int BodyColor(int bodyIndex)
            {
              // Transparent
              // BGRA Format
              int color = 0X00000000;
              switch(bodyIndex)
              {
    
                // Red
                case 0:
                color = 0Xffff0000;
                break;
                // Green
                case 1:
                color = 0Xff008000;
                break;
                // Dark Magenta
                case 2:
                color = 0Xff8b008b;
                break;
                // Blue
                case 3:
                color = 0Xff0000ff;
                break;
                // Purple
                case 4:
                color = 0Xff800080;
                break;
                // Orange
                case 5:
                color = 0Xffffa500;
                break;   
              }
  
              return color;
            }

            bool Contains(__global int* array, int arrayLength, int val)
            {
              bool found = false;
              for (int i = 0; i < arrayLength; i++)
              {
                if (array[i] == val)
                {
                  found = true;
                  break;
                }
              }
  
              return found;
            }  
        ";

        public static string distortImageCode = @"
            int Mod(int x, int y)
            {
              if (x < 0)
              {
                return Mod(x+y, y);
              }
  
              return x % y;
            }

            __kernel void DistortImage(__global char* originalImage, __global char* alteredImage, __global int* stride, __global double* effectSize)
            {
              int col = get_global_id(0);
              int row = get_global_id(1);

              double rowRadians = row * (M_PI / 180);
              int adjustedEffectSize = floor(20 * effectSize[0] * sin(12 * effectSize[0] * rowRadians));
  
              int colOffset = col * 4;
              int tmpColOffset = Mod(((col + adjustedEffectSize) * 4), stride[0]);
  
              int rowOffset = (row * stride[0]);
  
              int originalIndex = rowOffset + tmpColOffset;
              int alteredIndex = rowOffset + colOffset;
 
              alteredImage[alteredIndex] = originalImage[originalIndex];
              alteredImage[alteredIndex + 1] = originalImage[originalIndex + 1];
              alteredImage[alteredIndex + 2] = originalImage[originalIndex + 2];
              alteredImage[alteredIndex + 3] = originalImage[originalIndex + 3];
            }
        ";
    }
}

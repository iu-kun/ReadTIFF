using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1
{
    internal class ReadTIFFButton : Button
    {
        protected override async void OnClick()
        {
            var mapView = MapView.Active;
            IReadOnlyList<Layer> selectedLayerList = mapView.GetSelectedLayers();

            for (int cur = 0; cur < selectedLayerList.Count(); cur++)
            {
                Layer firstSelectedLayer = selectedLayerList[cur];
                await ReadorWriteTIFF(firstSelectedLayer);
            }

        }


        public static async Task ReadorWriteTIFF(Layer firstSelectedLayer)
        {
            try
            {
                await QueuedTask.Run(() =>
                {

                    RasterLayer currentRasterLayer = firstSelectedLayer as RasterLayer;
                    Raster inputRaster = currentRasterLayer.GetRaster();
                    BasicRasterDataset basicRasterDataset = inputRaster.GetRasterDataset();

                    //获取文件名
                    string layer_name = currentRasterLayer.Name;
                    string[] strs1 = layer_name.Split('.');
                    string name = strs1[0];

                    //获取文件路径
                    string GetCompleteName = basicRasterDataset.GetCompleteName();
                    string[] strs = GetCompleteName.Split('.');
                    string path = strs[0];

                    if (!(basicRasterDataset is RasterDataset))
                    {
                        MessageBox.Show("No Raster Layers selected. Please select one Raster layer.");
                        return;
                    }
                    RasterDataset rasterDataset = basicRasterDataset as RasterDataset;
                    Raster workingRater = rasterDataset.CreateFullRaster();

                    DirectoryInfo dir = new DirectoryInfo(path);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //读取栅格值
                    //var value = workingRater.GetPixelValue(0, 0, 0);
                  

                    //修改栅格值并保存
                    int width = workingRater.GetWidth();
                    int height = workingRater.GetHeight();

                    PixelBlock currentPixelBlock = workingRater.CreatePixelBlock(width, height);

                    int plane_num = currentPixelBlock.GetPlaneCount();
                    for (int plane = 0; plane < plane_num; plane++)
                    {
                        Array p = currentPixelBlock.GetPixelData(plane, true);
                        int pixels_row = p.GetLength(0);
                        int pixels_col = p.GetLength(1);
                        for (int i = 0; i < pixels_row; i++)
                        {
                            for (int j = 0; j < pixels_col; j++)
                            {
                                p.SetValue(0, i, j);
                            }
                        }
                        currentPixelBlock.SetPixelData(plane, p);
                    }
                    workingRater.Write(0, 0, currentPixelBlock);
                    workingRater.Refresh();


                    FileSystemConnectionPath outputConnectionPath = new FileSystemConnectionPath(
                    new System.Uri(path), FileSystemDatastoreType.Raster);
                    FileSystemDatastore outputFileSytemDataStore = new FileSystemDatastore(outputConnectionPath);
                    RasterStorageDef rasterStorageDef = new RasterStorageDef();
                    rasterStorageDef.SetPyramidLevel(0);
                    string outputname = "new_" + name + ".tif";
                    workingRater.SaveAs(outputname, outputFileSytemDataStore, "TIFF", rasterStorageDef);

                    //保存单个波段
                    Raster raster = workingRater.GetBand(0).CreateDefaultRaster();
                    string band_name ="band.tif";
                    raster.SaveAs(band_name, outputFileSytemDataStore, "TIFF", rasterStorageDef);


                });
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception caught while trying to add layer: " + e.Message);
                return;
            }
        }

    }
}

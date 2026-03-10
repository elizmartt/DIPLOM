import pydicom
import numpy as np
from PIL import Image
import logging

logger = logging.getLogger(__name__)


class DicomProcessor:

    def __init__(self):
        pass

    def parse_dicom(self, dicom_file_path):

        try:
            logger.info(f"Loading DICOM file: {dicom_file_path}")
            ds = pydicom.dcmread(dicom_file_path)


            def safe_get_numeric(ds, tag, default=0, value_type=int):
                try:
                    value = ds.get(tag)
                    if value is None:
                        return default


                    if isinstance(value, pydicom.multival.MultiValue):
                        value = value[0] if len(value) > 0 else default


                    return value_type(value)
                except (ValueError, TypeError, IndexError):
                    return default

            metadata = {
                'patient_id': str(ds.get('PatientID', 'Unknown')),
                'study_date': str(ds.get('StudyDate', 'Unknown')),
                'modality': str(ds.get('Modality', 'Unknown')),
                'rows': safe_get_numeric(ds, 'Rows', 0, int),
                'columns': safe_get_numeric(ds, 'Columns', 0, int),
                'window_center': safe_get_numeric(ds, 'WindowCenter', 0, int),
                'window_width': safe_get_numeric(ds, 'WindowWidth', 0, int),
                'rescale_intercept': safe_get_numeric(ds, 'RescaleIntercept', 0, float),
                'rescale_slope': safe_get_numeric(ds, 'RescaleSlope', 1, float),
            }

            logger.info(f"DICOM metadata extracted: {metadata}")
            return ds, metadata

        except Exception as e:
            logger.error(f"Error parsing DICOM file: {str(e)}")
            raise

    def apply_windowing(self, pixel_array, window_center, window_width):

        try:

            pixel_array = pixel_array.astype(np.float32)


            img_min = float(window_center - window_width // 2)
            img_max = float(window_center + window_width // 2)


            windowed = np.clip(pixel_array, img_min, img_max)



            if img_max - img_min > 0:
                windowed = ((windowed - img_min) / (img_max - img_min) * 255.0)
            else:
                windowed = np.full_like(windowed, 127.5)


            windowed = np.clip(windowed, 0, 255)
            return windowed.astype(np.uint8)

        except Exception as e:
            logger.error(f"Error applying windowing: {str(e)}")

            try:
                pixel_array = pixel_array.astype(np.float32)
                pix_min = pixel_array.min()
                pix_max = pixel_array.max()
                if pix_max - pix_min > 0:
                    normalized = ((pixel_array - pix_min) / (pix_max - pix_min) * 255.0)
                else:
                    normalized = np.full_like(pixel_array, 127.5)
                return np.clip(normalized, 0, 255).astype(np.uint8)
            except:

                logger.error("Fallback normalization also failed")
                return np.zeros_like(pixel_array, dtype=np.uint8)

    def extract_image_for_ai(self, dicom_file_path):

        try:

            ds, metadata = self.parse_dicom(dicom_file_path)


            pixel_array = ds.pixel_array


            if metadata['window_center'] and metadata['window_width']:
                pixel_array = self.apply_windowing(
                    pixel_array,
                    metadata['window_center'],
                    metadata['window_width']
                )
                logger.info(f"Applied DICOM windowing: C={metadata['window_center']}, W={metadata['window_width']}")
            else:
                pixel_array = ((pixel_array - pixel_array.min()) /
                               (pixel_array.max() - pixel_array.min()) * 255).astype(np.uint8)
                logger.info("Applied default normalization (no windowing metadata)")

            image = Image.fromarray(pixel_array)

            if image.mode != 'RGB':
                image = image.convert('RGB')
                logger.info("Converted grayscale DICOM to RGB")

            logger.info(f"✓ Extracted image from DICOM: {image.size[0]}x{image.size[1]}")

            return image, metadata

        except Exception as e:
            logger.error(f"Error extracting image from DICOM: {str(e)}")
            raise


def load_image_from_path(file_path):

    try:
        if file_path.lower().endswith('.dcm'):
            logger.info(f"Loading DICOM file: {file_path}")
            processor = DicomProcessor()
            image, metadata = processor.extract_image_for_ai(file_path)
            metadata['is_dicom'] = True
            return image, metadata
        else:
            logger.info(f"Loading standard image: {file_path}")
            image = Image.open(file_path).convert('RGB')
            metadata = {
                'is_dicom': False,
                'format': image.format,
                'size': image.size
            }
            return image, metadata

    except Exception as e:
        logger.error(f"Error loading image from {file_path}: {str(e)}")
        raise
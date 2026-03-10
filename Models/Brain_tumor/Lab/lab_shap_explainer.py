

import shap
import numpy as np
import json
from typing import Dict, List, Optional
import logging

logger = logging.getLogger(__name__)


class LabResultsShapExplainer:

    def __init__(self, model, feature_names: List[str], background_data: Optional[np.ndarray] = None):


        self.model = model
        self.feature_names = feature_names

        model_type = type(model).__name__

        try:
            if model_type in ['LogisticRegression', 'LinearSVC', 'Ridge', 'Lasso']:
                self.explainer = shap.LinearExplainer(
                    self.model,
                    background_data if background_data is not None else np.zeros((1, len(feature_names)))
                )
                logger.info(f"Created LinearExplainer for {model_type}")
            elif model_type in ['SVC', 'SVR'] or hasattr(model, 'decision_function'):
                if background_data is None:
                    background_data = np.zeros((10, len(feature_names)))  # Minimal background
                self.explainer = shap.KernelExplainer(
                    model.predict_proba if hasattr(model, 'predict_proba') else model.decision_function,
                    background_data
                )
                logger.info(f"Created KernelExplainer for {model_type}")
            else:
                if background_data is None:
                    background_data = np.zeros((10, len(feature_names)))
                self.explainer = shap.KernelExplainer(
                    model.predict_proba,
                    background_data
                )
                logger.info(f"Created KernelExplainer for {model_type}")

        except Exception as e:
            logger.error(f"Failed to create explainer: {e}", exc_info=True)
            raise

    def explain_prediction(
        self,
        lab_data: np.ndarray,
        predicted_class: int,
        reference_ranges: Optional[Dict[str, tuple]] = None
    ) -> Dict:

        try:
            if lab_data.ndim == 1:
                lab_data = lab_data.reshape(1, -1)

            shap_values = self.explainer.shap_values(lab_data)

            if isinstance(shap_values, list):
                shap_values_for_class = shap_values[predicted_class][0]
            elif shap_values.ndim == 3:
                shap_values_for_class = shap_values[0, :, predicted_class]
            else:
                shap_values_for_class = shap_values[0]

            if hasattr(self.explainer, 'expected_value'):
                if isinstance(self.explainer.expected_value, (list, np.ndarray)):
                    base_value = float(self.explainer.expected_value[predicted_class])
                else:
                    base_value = float(self.explainer.expected_value)
            else:
                base_value = 0.0

            feature_impacts = []
            for i, (feature_name, shap_value, lab_value) in enumerate(
                zip(self.feature_names, shap_values_for_class, lab_data[0])
            ):
                status = 'unknown'
                if reference_ranges and feature_name in reference_ranges:
                    min_val, max_val = reference_ranges[feature_name]
                    if lab_value < min_val:
                        status = 'low'
                    elif lab_value > max_val:
                        status = 'high'
                    else:
                        status = 'normal'

                feature_impacts.append({
                    'feature_name': feature_name,
                    'lab_value': float(lab_value),
                    'shap_value': float(shap_value),
                    'abs_shap_value': float(abs(shap_value)),
                    'impact': 'positive' if shap_value > 0 else 'negative' if shap_value < 0 else 'neutral',
                    'status': status
                })

            feature_impacts.sort(key=lambda x: x['abs_shap_value'], reverse=True)

            total_positive_impact = sum(f['shap_value'] for f in feature_impacts if f['shap_value'] > 0)
            total_negative_impact = sum(f['shap_value'] for f in feature_impacts if f['shap_value'] < 0)

            top_positive = [f for f in feature_impacts if f['shap_value'] > 0.01][:3]
            top_negative = [f for f in feature_impacts if f['shap_value'] < -0.01][:3]

            summary_text = self._generate_summary(
                top_positive,
                top_negative,
                predicted_class,
                feature_impacts
            )

            return {
                'base_value': base_value,
                'predicted_class': int(predicted_class),
                'feature_impacts': feature_impacts,
                'top_positive_features': top_positive,
                'top_negative_features': top_negative,
                'total_positive_impact': float(total_positive_impact),
                'total_negative_impact': float(total_negative_impact),
                'summary': summary_text,
                'metadata': {
                    'model_type': type(self.model).__name__,
                    'num_features': len(self.feature_names),
                    'explanation_method': type(self.explainer).__name__
                }
            }

        except Exception as e:
            logger.error(f"SHAP explanation failed: {e}", exc_info=True)
            return {
                'error': str(e),
                'feature_impacts': []
            }

    def _generate_summary(
        self,
        top_positive: List[Dict],
        top_negative: List[Dict],
        predicted_class: int,
        all_features: List[Dict]
    ) -> str:

        class_names = {
            0: 'Glioma',
            1: 'Meningioma',
            2: 'Pituitary Tumor',
            3: 'Lung Cancer - Benign',
            4: 'Lung Cancer - Malignant'
        }

        disease = class_names.get(predicted_class, f'Class {predicted_class}')

        summary_parts = [f"Laboratory results indicate {disease}. Key biomarkers:"]

        if top_positive:
            summary_parts.append("\n\nBiomarkers SUPPORTING this diagnosis:")
            for feature in top_positive:
                status_info = f" ({feature['status']})" if feature['status'] != 'unknown' else ""
                summary_parts.append(
                    f"  • {feature['feature_name']}: {feature['lab_value']:.2f}{status_info} "
                    f"(impact: +{feature['shap_value']:.3f})"
                )

        if top_negative:
            summary_parts.append("\n\nBiomarkers CONTRADICTING this diagnosis:")
            for feature in top_negative:
                status_info = f" ({feature['status']})" if feature['status'] != 'unknown' else ""
                summary_parts.append(
                    f"  • {feature['feature_name']}: {feature['lab_value']:.2f}{status_info} "
                    f"(impact: {feature['shap_value']:.3f})"
                )

        abnormal_labs = [f for f in all_features if f['status'] in ['low', 'high']]
        if abnormal_labs:
            summary_parts.append(f"\n\n{len(abnormal_labs)} abnormal lab value(s) detected.")

        return ''.join(summary_parts)

    def get_json_for_database(self, explanation: Dict) -> str:
        return json.dumps(explanation, ensure_ascii=False, indent=2)





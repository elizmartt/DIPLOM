

import shap
import numpy as np
import json
from typing import Dict, List, Tuple
import logging

logger = logging.getLogger(__name__)


class SymptomsShapExplainer:


    def __init__(self, model, feature_names: List[str]):

        self.model = model
        self.feature_names = feature_names

        try:
            self.explainer = shap.TreeExplainer(self.model)
            logger.info("Created TreeExplainer for model")
        except Exception as e:
            logger.warning(f"TreeExplainer failed: {e}, falling back to KernelExplainer")
            self.explainer = shap.KernelExplainer(
                self.model.predict_proba,
                np.zeros((1, len(feature_names)))  # Background dataset
            )

    def explain_prediction(
            self,
            symptoms_data: np.ndarray,
            predicted_class: int
    ) -> Dict:

        try:
            if symptoms_data.ndim == 1:
                symptoms_data = symptoms_data.reshape(1, -1)

            shap_values = self.explainer.shap_values(symptoms_data)

            if isinstance(shap_values, list):
                shap_values_for_class = shap_values[predicted_class]
                while shap_values_for_class.ndim > 1:
                    shap_values_for_class = shap_values_for_class[0]
            elif shap_values.ndim == 3:
                shap_values_for_class = shap_values[0, :, predicted_class]
            elif shap_values.ndim == 2:
                shap_values_for_class = shap_values[0]
            else:
                shap_values_for_class = shap_values

            if hasattr(self.explainer, 'expected_value'):
                if isinstance(self.explainer.expected_value, (list, np.ndarray)):
                    base_value = float(self.explainer.expected_value[predicted_class])
                else:
                    base_value = float(self.explainer.expected_value)
            else:
                base_value = 0.0

            feature_impacts = []
            for i, (feature_name, shap_value, feature_value) in enumerate(
                    zip(self.feature_names, shap_values_for_class, symptoms_data[0])
            ):
                feature_impacts.append({
                    'feature_name': feature_name,
                    'feature_value': int(feature_value),
                    'shap_value': float(shap_value),
                    'abs_shap_value': float(abs(shap_value)),
                    'impact': 'positive' if shap_value > 0 else 'negative' if shap_value < 0 else 'neutral'
                })

            feature_impacts.sort(key=lambda x: x['abs_shap_value'], reverse=True)

            total_positive_impact = sum(f['shap_value'] for f in feature_impacts if f['shap_value'] > 0)
            total_negative_impact = sum(f['shap_value'] for f in feature_impacts if f['shap_value'] < 0)

            top_positive = [f for f in feature_impacts if f['shap_value'] > 0.01][:3]
            top_negative = [f for f in feature_impacts if f['shap_value'] < -0.01][:3]

            summary_text = self._generate_summary(
                top_positive,
                top_negative,
                predicted_class
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
                    'explanation_method': 'SHAP TreeExplainer'
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
            predicted_class: int
    ) -> str:

        class_names = {
            0: 'Glioma',
            1: 'Meningioma',
            2: 'Pituitary Tumor'
        }

        disease = class_names.get(predicted_class, f'Class {predicted_class}')

        summary_parts = [f"The model predicted {disease} based on the following symptoms:"]

        if top_positive:
            summary_parts.append("\n\nSymptoms that INCREASED the likelihood:")
            for feature in top_positive:
                if feature['feature_value'] == 1:
                    summary_parts.append(
                        f"  • {feature['feature_name']} (impact: +{feature['shap_value']:.3f})"
                    )

        if top_negative:
            summary_parts.append("\n\nSymptoms that DECREASED the likelihood:")
            for feature in top_negative:
                if feature['feature_value'] == 1:
                    summary_parts.append(
                        f"  • {feature['feature_name']} (impact: {feature['shap_value']:.3f})"
                    )

        return ''.join(summary_parts)

    def get_json_for_database(self, explanation: Dict) -> str:

        return json.dumps(explanation, ensure_ascii=False, indent=2)





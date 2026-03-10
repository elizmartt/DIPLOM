using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedicalDiagnostic.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    action_details = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => new { x.log_id, x.created_at });
                });

            migrationBuilder.CreateTable(
                name: "clinical_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    diagnosis_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prediction = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    probabilities = table.Column<string>(type: "jsonb", nullable: false),
                    processing_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    explainability_data = table.Column<string>(type: "jsonb", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinical_symptoms",
                columns: table => new
                {
                    symptom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symptoms = table.Column<string>(type: "jsonb", nullable: false),
                    blood_pressure = table.Column<string>(type: "text", nullable: true),
                    heart_rate = table.Column<int>(type: "integer", nullable: true),
                    temperature = table.Column<double>(type: "double precision", nullable: true),
                    smoking_history = table.Column<bool>(type: "boolean", nullable: true),
                    family_history = table.Column<string>(type: "jsonb", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_symptoms", x => x.symptom_id);
                });

            migrationBuilder.CreateTable(
                name: "diagnosis_cases",
                columns: table => new
                {
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: true, defaultValue: "pending"),
                    priority = table.Column<string>(type: "text", nullable: true),
                    doctor_diagnosis = table.Column<string>(type: "text", nullable: true),
                    doctor_notes = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diagnosis_cases", x => new { x.case_id, x.created_at });
                    table.UniqueConstraint("AK_diagnosis_cases_case_id", x => x.case_id);
                });

            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    specialization = table.Column<string>(type: "text", nullable: false),
                    hospital_affiliation = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctors", x => x.doctor_id);
                });

            migrationBuilder.CreateTable(
                name: "imaging_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    diagnosis_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prediction = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    probabilities = table.Column<string>(type: "jsonb", nullable: false),
                    processing_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    explainability_data = table.Column<string>(type: "jsonb", nullable: false),
                    grad_cam_overlay_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    grad_cam_heatmap_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    grad_cam_image_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imaging_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lab_tests",
                columns: table => new
                {
                    lab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lab_name = table.Column<string>(type: "text", nullable: false),
                    test_results = table.Column<string>(type: "jsonb", nullable: false),
                    reference_ranges = table.Column<string>(type: "jsonb", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_tests", x => x.lab_id);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    diagnosis_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prediction = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    probabilities = table.Column<string>(type: "jsonb", nullable: false),
                    processing_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    explainability_data = table.Column<string>(type: "jsonb", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laboratory_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_code = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    age = table.Column<int>(type: "integer", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.patient_id);
                });

            migrationBuilder.CreateTable(
                name: "unified_diagnosis_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    diagnosis_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    final_diagnosis = table.Column<string>(type: "text", nullable: false),
                    overall_confidence = table.Column<double>(type: "double precision", nullable: false),
                    ensemble_probabilities = table.Column<string>(type: "jsonb", nullable: false),
                    contributing_modules = table.Column<string[]>(type: "text[]", nullable: false),
                    risk_level = table.Column<string>(type: "text", nullable: false),
                    recommendations = table.Column<string[]>(type: "text[]", nullable: false),
                    explainability_summary = table.Column<string>(type: "jsonb", nullable: false),
                    total_processing_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "completed"),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unified_diagnosis_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medical_images",
                columns: table => new
                {
                    image_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_type = table.Column<string>(type: "text", nullable: false),
                    scan_area = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    modality = table.Column<string>(type: "text", nullable: true),
                    study_uid = table.Column<string>(type: "text", nullable: true),
                    series_uid = table.Column<string>(type: "text", nullable: true),
                    instance_uid = table.Column<string>(type: "text", nullable: true),
                    study_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    series_description = table.Column<string>(type: "text", nullable: true),
                    series_number = table.Column<int>(type: "integer", nullable: true),
                    instance_number = table.Column<int>(type: "integer", nullable: true),
                    pixel_spacing = table.Column<string>(type: "text", nullable: true),
                    slice_thickness = table.Column<decimal>(type: "numeric", nullable: true),
                    window_center = table.Column<int>(type: "integer", nullable: true),
                    window_width = table.Column<int>(type: "integer", nullable: true),
                    rows = table.Column<int>(type: "integer", nullable: true),
                    columns = table.Column<int>(type: "integer", nullable: true),
                    dicom_metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    is_preprocessed = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_images", x => x.image_id);
                    table.ForeignKey(
                        name: "FK_medical_images_diagnosis_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "diagnosis_cases",
                        principalColumn: "case_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_action",
                table: "audit_log",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_case_id",
                table: "audit_log",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_created_at",
                table: "audit_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_doctor_id",
                table: "audit_log",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_results_diagnosis_case_id",
                table: "clinical_results",
                column: "diagnosis_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_symptoms_case_id",
                table: "clinical_symptoms",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_cases_created_at",
                table: "diagnosis_cases",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_cases_doctor_id",
                table: "diagnosis_cases",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_cases_patient_id",
                table: "diagnosis_cases",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_cases_status",
                table: "diagnosis_cases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_email",
                table: "doctors",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_imaging_results_diagnosis_case_id",
                table: "imaging_results",
                column: "diagnosis_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_lab_tests_case_id",
                table: "lab_tests",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_lab_tests_test_date",
                table: "lab_tests",
                column: "test_date");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_results_diagnosis_case_id",
                table: "laboratory_results",
                column: "diagnosis_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_images_case_id",
                table: "medical_images",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_patient_code",
                table: "patients",
                column: "patient_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unified_diagnosis_results_diagnosis_case_id",
                table: "unified_diagnosis_results",
                column: "diagnosis_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_unified_diagnosis_results_final_diagnosis",
                table: "unified_diagnosis_results",
                column: "final_diagnosis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "clinical_results");

            migrationBuilder.DropTable(
                name: "clinical_symptoms");

            migrationBuilder.DropTable(
                name: "doctors");

            migrationBuilder.DropTable(
                name: "imaging_results");

            migrationBuilder.DropTable(
                name: "lab_tests");

            migrationBuilder.DropTable(
                name: "laboratory_results");

            migrationBuilder.DropTable(
                name: "medical_images");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "unified_diagnosis_results");

            migrationBuilder.DropTable(
                name: "diagnosis_cases");
        }
    }
}

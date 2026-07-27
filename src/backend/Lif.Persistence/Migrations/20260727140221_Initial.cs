using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lif.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.EnsureSchema(
                name: "curriculum");

            migrationBuilder.EnsureSchema(
                name: "learner");

            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "assessment_blueprints",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    cefr_level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_blueprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "concept_mastery",
                schema: "learner",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mastery = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<int>(type: "integer", nullable: false),
                    streak_wrong = table.Column<int>(type: "integer", nullable: false),
                    provisional = table.Column<bool>(type: "boolean", nullable: false),
                    last_practiced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_mastery", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "concepts",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    cefr_level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    difficulty = table.Column<short>(type: "smallint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    learning_objective = table.Column<string>(type: "text", nullable: false),
                    explanation_md = table.Column<string>(type: "text", nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    mastery_threshold = table.Column<double>(type: "double precision", nullable: false),
                    importance = table.Column<double>(type: "double precision", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    required_vocab_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "curriculum_versions",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_curriculum_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    spec = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mistakes",
                schema: "learner",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill = table.Column<string>(type: "text", nullable: false),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wrong = table.Column<string>(type: "text", nullable: true),
                    right = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mistakes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "learner",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    ui_language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    target_cefr = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    current_cefr = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    minutes_per_day = table.Column<int>(type: "integer", nullable: false),
                    curriculum_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "review_items",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    item_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    fsrs_stability = table.Column<double>(type: "double precision", nullable: false),
                    fsrs_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    last_review_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    priority = table.Column<double>(type: "double precision", nullable: false),
                    postponed_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "review_logs",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    elapsed_days = table.Column<double>(type: "double precision", nullable: false),
                    scheduled_days = table.Column<double>(type: "double precision", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    time_budget_minutes = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<string>(type: "jsonb", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cefr_level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    culture_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vocab_entries",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lemma = table.Column<string>(type: "text", nullable: false),
                    pos = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cefr_level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    difficulty = table.Column<short>(type: "smallint", nullable: false),
                    gender = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    plural = table.Column<string>(type: "text", nullable: true),
                    ipa = table.Column<string>(type: "text", nullable: true),
                    frequency_rank = table.Column<int>(type: "integer", nullable: true),
                    separable = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocab_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "word_states",
                schema: "learner",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vocab_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    lapse_count = table.Column<int>(type: "integer", nullable: false),
                    leech = table.Column<bool>(type: "boolean", nullable: false),
                    personal_note = table.Column<string>(type: "text", nullable: true),
                    context_sentence = table.Column<string>(type: "text", nullable: true),
                    last_review_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "concept_common_mistakes",
                schema: "curriculum",
                columns: table => new
                {
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wrong = table.Column<string>(type: "text", nullable: false),
                    right = table.Column<string>(type: "text", nullable: false),
                    explanation = table.Column<string>(type: "text", nullable: false),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_common_mistakes", x => new { x.concept_id, x.id });
                    table.ForeignKey(
                        name: "fk_concept_common_mistakes_concepts_concept_id",
                        column: x => x.concept_id,
                        principalSchema: "curriculum",
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concept_dependencies",
                schema: "curriculum",
                columns: table => new
                {
                    prerequisite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dep_type = table.Column<string>(type: "text", nullable: false),
                    min_mastery = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_dependencies", x => new { x.concept_id, x.prerequisite_id });
                    table.ForeignKey(
                        name: "fk_concept_dependencies_concepts_concept_id",
                        column: x => x.concept_id,
                        principalSchema: "curriculum",
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concept_examples",
                schema: "curriculum",
                columns: table => new
                {
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    german = table.Column<string>(type: "text", nullable: false),
                    english = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_examples", x => new { x.concept_id, x.order_index });
                    table.ForeignKey(
                        name: "fk_concept_examples_concepts_concept_id",
                        column: x => x.concept_id,
                        principalSchema: "curriculum",
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_activities_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "learning",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_templates",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concept_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_lesson_templates_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "curriculum",
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "word_senses",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vocab_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sense_order = table.Column<int>(type: "integer", nullable: false),
                    meaning_en = table.Column<string>(type: "text", nullable: false),
                    example_de = table.Column<string>(type: "text", nullable: false),
                    example_en = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_senses", x => x.id);
                    table.ForeignKey(
                        name: "fk_word_senses_vocab_entries_vocab_entry_id",
                        column: x => x.vocab_entry_id,
                        principalSchema: "curriculum",
                        principalTable: "vocab_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_instances",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    spec = table.Column<string>(type: "jsonb", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    gen_meta = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_instances_activities_activity_id",
                        column: x => x.activity_id,
                        principalSchema: "learning",
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response = table.Column<string>(type: "text", nullable: false),
                    outcome = table.Column<double>(type: "double precision", nullable: false),
                    hints_used = table.Column<int>(type: "integer", nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: true),
                    evaluated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_attempts_exercise_instances_exercise_instance_id",
                        column: x => x.exercise_instance_id,
                        principalSchema: "learning",
                        principalTable: "exercise_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_session_id_order_index",
                schema: "learning",
                table: "activities",
                columns: new[] { "session_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assessment_blueprints_version_id_kind_cefr_level",
                schema: "curriculum",
                table: "assessment_blueprints",
                columns: new[] { "version_id", "kind", "cefr_level" });

            migrationBuilder.CreateIndex(
                name: "ix_attempts_exercise_instance_id",
                schema: "learning",
                table: "attempts",
                column: "exercise_instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_learner_id_occurred_at",
                schema: "learning",
                table: "attempts",
                columns: new[] { "learner_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_concept_mastery_learner_id_concept_id",
                schema: "learner",
                table: "concept_mastery",
                columns: new[] { "learner_id", "concept_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_concepts_unit_id",
                schema: "curriculum",
                table: "concepts",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_concepts_version_id_code",
                schema: "curriculum",
                table: "concepts",
                columns: new[] { "version_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_curriculum_versions_language_code_version",
                schema: "curriculum",
                table: "curriculum_versions",
                columns: new[] { "language_code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercise_instances_activity_id_order_index",
                schema: "learning",
                table: "exercise_instances",
                columns: new[] { "activity_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercises_version_id_code",
                schema: "curriculum",
                table: "exercises",
                columns: new[] { "version_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lesson_templates_unit_id_order_index",
                schema: "curriculum",
                table: "lesson_templates",
                columns: new[] { "unit_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mistakes_learner_id_concept_id",
                schema: "learner",
                table: "mistakes",
                columns: new[] { "learner_id", "concept_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mistakes_learner_id_occurred_at",
                schema: "learner",
                table: "mistakes",
                columns: new[] { "learner_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_review_items_due_at",
                schema: "scheduling",
                table: "review_items",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "ix_review_items_learner_id_kind_item_ref",
                schema: "scheduling",
                table: "review_items",
                columns: new[] { "learner_id", "kind", "item_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_logs_learner_id_reviewed_at",
                schema: "scheduling",
                table: "review_logs",
                columns: new[] { "learner_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_review_logs_review_item_id_reviewed_at",
                schema: "scheduling",
                table: "review_logs",
                columns: new[] { "review_item_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_learner_id_status",
                schema: "learning",
                table: "sessions",
                columns: new[] { "learner_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_units_version_id_cefr_level_order_index",
                schema: "curriculum",
                table: "units",
                columns: new[] { "version_id", "cefr_level", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vocab_entries_version_id_frequency_rank",
                schema: "curriculum",
                table: "vocab_entries",
                columns: new[] { "version_id", "frequency_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_vocab_entries_version_id_lemma_pos",
                schema: "curriculum",
                table: "vocab_entries",
                columns: new[] { "version_id", "lemma", "pos" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_senses_vocab_entry_id_sense_order",
                schema: "curriculum",
                table: "word_senses",
                columns: new[] { "vocab_entry_id", "sense_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_states_learner_id_status",
                schema: "learner",
                table: "word_states",
                columns: new[] { "learner_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_word_states_learner_id_vocab_entry_id",
                schema: "learner",
                table: "word_states",
                columns: new[] { "learner_id", "vocab_entry_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_blueprints",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "attempts",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "concept_common_mistakes",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "concept_dependencies",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "concept_examples",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "concept_mastery",
                schema: "learner");

            migrationBuilder.DropTable(
                name: "curriculum_versions",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "lesson_templates",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "mistakes",
                schema: "learner");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "learner");

            migrationBuilder.DropTable(
                name: "review_items",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "review_logs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "word_senses",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "word_states",
                schema: "learner");

            migrationBuilder.DropTable(
                name: "exercise_instances",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "concepts",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "units",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "vocab_entries",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "activities",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "learning");
        }
    }
}

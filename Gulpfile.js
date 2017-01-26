var gulp = require('gulp');
var changed = require('gulp-changed');
var concat = require('gulp-concat');
var rename = require('gulp-rename');
var uglify = require('gulp-uglify');
var cleanCSS = require('gulp-clean-css');
var ts = require('gulp-typescript');
var sourcemaps = require('gulp-sourcemaps');

gulp.task('ts', function () {
    var tsProject = ts.createProject('tsconfig.json');
    return tsProject.src()
        .pipe(sourcemaps.init({loadMaps: true}))
        .pipe(tsProject())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('js'));
});

gulp.task('scripts', ['ts'], function () {
    var SRC = 'js/**/*.js',
        DEST = 'static';

    return gulp.src(SRC)
        .pipe(changed(DEST))
        .pipe(sourcemaps.init({loadMaps: true}))
        .pipe(concat('scripts.js'))
        .pipe(gulp.dest(DEST))
        .pipe(rename('scripts.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write())
        .pipe(gulp.dest(DEST));
});

gulp.task('css', function () {
    var SRC = [
        'css/vendor/pure-layout.css',
        'css/vendor/solarized-light.css',
        'css/vendor/prism.css',
        'css/vendor/html5bp.css',
        'css/main.css'];

    gulp.src(SRC)
        .pipe(sourcemaps.init())
        .pipe(cleanCSS({compatibility: 'ie8'}))
        .pipe(concat('styles.min.css'))
        .pipe(sourcemaps.write())
        .pipe(gulp.dest('static'));
});

gulp.task('default', ['scripts', 'css']);

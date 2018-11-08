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
    var SRC = [
        'node_modules/clipboard/dist/clipboard.min.js',
        'node_modules/jquery/dist/jquery.min.js',
        'node_modules/magnific-popup/dist/jquery.magnific-popup.min.js',
        'node_modules/mathjax/MathJax.js',   // move this to something that copies whole node_module to the output site
        'js/**/*.js'];
    var DEST = 'static';

    return gulp.src(SRC)
        .pipe(changed(DEST))
        .pipe(sourcemaps.init({loadMaps: true}))
        .pipe(concat('scripts.js'))
        .pipe(gulp.dest(DEST))
        .pipe(rename('scripts.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(DEST));
});

gulp.task('fonts', function() {
    var SRC = 'node_modules/font-awesome/fonts/*',
        DEST = 'fonts';
    
    return gulp.src(SRC)
        .pipe(gulp.dest(DEST));
});

gulp.task('css', ['fonts'], function () {
    var SRC = [
        'node_modules/ace-css/css/ace.min.css',
        'node_modules/font-awesome/css/font-awesome.min.css',
        'node_modules/magnific-popup/dist/magnific-popup.css',
        'node_modules/normalize.css/normalize.css',
        'node_modules/purecss/build/pure-min.css',
        'node_modules/purecss/build/grids-responsive-min.css',
        'css/vendor/*.css',
        'css/main.css'];

    gulp.src(SRC)
        .pipe(sourcemaps.init())
        .pipe(concat('styles.css'))
        .pipe(gulp.dest('static'))
        .pipe(cleanCSS({compatibility: 'ie8'}))
        .pipe(rename('styles.min.css'))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('static'));
});

gulp.task('default', ['scripts', 'css']);

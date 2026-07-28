from __future__ import annotations

import os
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    Flowable,
    Image,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "Docs"
IMAGES = DOCS / "GuideImages"
OUTPUT = DOCS / "新人安装与拉取项目教程_图文版.pdf"

REPO_URL = "https://github.com/mgc1159735525/Minglu.git"
PROJECT_PATH = r"D:\paotuan\MingLuUnity"
UNITY_VERSION = "2022.3.62f3"

FONT_REGULAR = "MingLuCN"
FONT_BOLD = "MingLuCNBold"
FONT_CODE = "MingLuCode"


def register_fonts() -> None:
    fonts_dir = Path(os.environ.get("WINDIR", r"C:\Windows")) / "Fonts"
    regular = fonts_dir / "simhei.ttf"
    if not regular.exists():
        regular = fonts_dir / "NotoSansSC-VF.ttf"
    if not regular.exists():
        raise FileNotFoundError("未找到中文字体 simhei.ttf 或 NotoSansSC-VF.ttf")

    pdfmetrics.registerFont(TTFont(FONT_REGULAR, str(regular)))
    pdfmetrics.registerFont(TTFont(FONT_BOLD, str(regular)))
    pdfmetrics.registerFont(TTFont(FONT_CODE, str(regular)))


def style_sheet():
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle(
            "title",
            parent=base["Title"],
            fontName=FONT_BOLD,
            fontSize=25,
            leading=34,
            alignment=TA_CENTER,
            textColor=colors.HexColor("#24160e"),
            spaceAfter=10,
        ),
        "subtitle": ParagraphStyle(
            "subtitle",
            parent=base["Normal"],
            fontName=FONT_REGULAR,
            fontSize=11.5,
            leading=18,
            alignment=TA_CENTER,
            textColor=colors.HexColor("#5f4a32"),
        ),
        "h1": ParagraphStyle(
            "h1",
            parent=base["Heading1"],
            fontName=FONT_BOLD,
            fontSize=17,
            leading=24,
            textColor=colors.HexColor("#36200f"),
            spaceBefore=4,
            spaceAfter=8,
        ),
        "h2": ParagraphStyle(
            "h2",
            parent=base["Heading2"],
            fontName=FONT_BOLD,
            fontSize=12.5,
            leading=18,
            textColor=colors.HexColor("#7a4f1f"),
            spaceBefore=6,
            spaceAfter=4,
        ),
        "body": ParagraphStyle(
            "body",
            parent=base["BodyText"],
            fontName=FONT_REGULAR,
            fontSize=9.7,
            leading=15,
            textColor=colors.HexColor("#2b2119"),
            spaceAfter=5,
        ),
        "caption": ParagraphStyle(
            "caption",
            parent=base["BodyText"],
            fontName=FONT_REGULAR,
            fontSize=8,
            leading=11,
            alignment=TA_CENTER,
            textColor=colors.HexColor("#6b5a48"),
        ),
    }


def wrap_text(text: str, chars: int) -> list[str]:
    lines: list[str] = []
    for raw in text.splitlines():
        if not raw:
            lines.append("")
            continue
        current = ""
        width = 0.0
        for ch in raw:
            unit = 0.55 if ord(ch) < 128 else 1.0
            if width + unit > chars and current:
                last_space = max(current.rfind(" "), current.rfind("、"))
                if last_space > 0 and len(current) - last_space <= 16:
                    lines.append(current[:last_space].rstrip())
                    current = current[last_space + 1 :] + ch
                    width = sum(0.55 if ord(c) < 128 else 1.0 for c in current)
                else:
                    lines.append(current)
                    current = ch
                    width = unit
            else:
                current += ch
                width += unit
        if current:
            lines.append(current)
    return lines


class CodeBlock(Flowable):
    def __init__(self, text: str, width: float | None = None, font_size: float = 8.2):
        super().__init__()
        self.text = text.strip("\n")
        self.requested_width = width
        self.font_size = font_size
        self.leading = font_size + 4
        self.pad_x = 9
        self.pad_y = 7
        self.lines: list[str] = []

    def wrap(self, avail_width, avail_height):
        self.width = self.requested_width or avail_width
        chars = max(24, int((self.width - self.pad_x * 2) / (self.font_size * 0.55)))
        self.lines = wrap_text(self.text, chars)
        self.height = self.pad_y * 2 + self.leading * max(1, len(self.lines))
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.saveState()
        c.setFillColor(colors.HexColor("#17130f"))
        c.roundRect(0, 0, self.width, self.height, 5, stroke=0, fill=1)
        c.setStrokeColor(colors.HexColor("#b88635"))
        c.setLineWidth(0.6)
        c.roundRect(0, 0, self.width, self.height, 5, stroke=1, fill=0)
        c.setFont(FONT_CODE, self.font_size)
        c.setFillColor(colors.HexColor("#f7e6c3"))
        y = self.height - self.pad_y - self.font_size
        for line in self.lines:
            c.drawString(self.pad_x, y, line)
            y -= self.leading
        c.restoreState()


class Callout(Flowable):
    def __init__(self, title: str, body: str, tone: str = "gold"):
        super().__init__()
        self.title = title
        self.body = body
        self.tone = tone
        self.lines: list[str] = []

    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.lines = wrap_text(self.body, int(avail_width / 8.4))
        self.height = 22 + len(self.lines) * 13 + 14
        return self.width, self.height

    def draw(self):
        palette = {
            "gold": ("#fff5dc", "#c28c2d", "#5a3719"),
            "red": ("#fff0eb", "#b85c3f", "#5a1f17"),
            "green": ("#eff8ef", "#57905b", "#173c1b"),
            "blue": ("#eef5ff", "#4b79ad", "#1b3452"),
        }
        fill, stroke, text = palette.get(self.tone, palette["gold"])
        c = self.canv
        c.saveState()
        c.setFillColor(colors.HexColor(fill))
        c.setStrokeColor(colors.HexColor(stroke))
        c.setLineWidth(0.9)
        c.roundRect(0, 0, self.width, self.height, 6, stroke=1, fill=1)
        c.setFont(FONT_BOLD, 10)
        c.setFillColor(colors.HexColor(text))
        c.drawString(10, self.height - 17, self.title)
        c.setFont(FONT_REGULAR, 8.5)
        y = self.height - 33
        for line in self.lines:
            c.drawString(10, y, line)
            y -= 13
        c.restoreState()


class StepMap(Flowable):
    def __init__(self, steps: list[tuple[str, str]]):
        super().__init__()
        self.steps = steps

    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.height = 165
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.saveState()
        box_w = (self.width - 24) / 3
        box_h = 47
        y0 = self.height - box_h
        for i, (title, desc) in enumerate(self.steps):
            row = i // 3
            col = i % 3
            x = col * (box_w + 12)
            y = y0 - row * (box_h + 21)
            c.setFillColor(colors.HexColor("#fff7e8"))
            c.setStrokeColor(colors.HexColor("#b88635"))
            c.setLineWidth(1)
            c.roundRect(x, y, box_w, box_h, 7, stroke=1, fill=1)
            c.setFillColor(colors.HexColor("#6e3527"))
            c.circle(x + 15, y + box_h - 17, 9, stroke=0, fill=1)
            c.setFillColor(colors.white)
            c.setFont(FONT_BOLD, 8)
            c.drawCentredString(x + 15, y + box_h - 20, str(i + 1))
            c.setFillColor(colors.HexColor("#3b2414"))
            c.setFont(FONT_BOLD, 9.5)
            c.drawString(x + 30, y + box_h - 16, title)
            c.setFont(FONT_REGULAR, 7.7)
            c.setFillColor(colors.HexColor("#62513d"))
            for line_i, line in enumerate(wrap_text(desc, int(box_w / 6.2))[:2]):
                c.drawString(x + 12, y + 15 - line_i * 10, line)
            if i < len(self.steps) - 1 and col < 2:
                c.setStrokeColor(colors.HexColor("#b88635"))
                c.line(x + box_w + 2, y + box_h / 2, x + box_w + 10, y + box_h / 2)
        c.restoreState()


class CardGrid(Flowable):
    def __init__(self, cards: list[tuple[str, str]], columns: int = 2, tone: str = "plain"):
        super().__init__()
        self.cards = cards
        self.columns = columns
        self.tone = tone
        self.card_lines: list[list[str]] = []

    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        gap = 10
        self.card_w = (avail_width - gap * (self.columns - 1)) / self.columns
        self.card_lines = []
        heights = []
        for _, body in self.cards:
            lines = wrap_text(body, int(self.card_w / 7.3))
            self.card_lines.append(lines)
            heights.append(31 + len(lines) * 12)
        self.row_heights = []
        for i in range(0, len(heights), self.columns):
            self.row_heights.append(max(heights[i : i + self.columns]))
        self.height = sum(self.row_heights) + gap * (len(self.row_heights) - 1)
        return self.width, self.height

    def draw(self):
        c = self.canv
        gap = 10
        y = self.height
        palette = ("#fffaf0", "#d4a348", "#3a2414")
        if self.tone == "blue":
            palette = ("#f1f6ff", "#6e8fb8", "#1c334d")
        if self.tone == "green":
            palette = ("#f1f8ef", "#6d9a64", "#173c1b")
        for row, row_h in enumerate(self.row_heights):
            y -= row_h
            for col in range(self.columns):
                idx = row * self.columns + col
                if idx >= len(self.cards):
                    continue
                x = col * (self.card_w + gap)
                title, _ = self.cards[idx]
                c.setFillColor(colors.HexColor(palette[0]))
                c.setStrokeColor(colors.HexColor(palette[1]))
                c.setLineWidth(0.8)
                c.roundRect(x, y, self.card_w, row_h, 6, stroke=1, fill=1)
                c.setFillColor(colors.HexColor(palette[2]))
                c.setFont(FONT_BOLD, 9.3)
                c.drawString(x + 9, y + row_h - 17, title)
                c.setFont(FONT_REGULAR, 7.9)
                c.setFillColor(colors.HexColor("#5f5246"))
                line_y = y + row_h - 31
                for line in self.card_lines[idx]:
                    c.drawString(x + 9, line_y, line)
                    line_y -= 12
            y -= gap


class BranchDiagram(Flowable):
    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.height = 116
        return self.width, self.height

    def draw_node(self, c, x, y, title, body, fill):
        c.setFillColor(colors.HexColor(fill))
        c.setStrokeColor(colors.HexColor("#b88635"))
        c.roundRect(x, y, 96, 43, 6, stroke=1, fill=1)
        c.setFillColor(colors.HexColor("#27180f"))
        c.setFont(FONT_BOLD, 8.6)
        c.drawCentredString(x + 48, y + 25, title)
        c.setFont(FONT_REGULAR, 7)
        c.setFillColor(colors.HexColor("#5b4b3d"))
        c.drawCentredString(x + 48, y + 12, body)

    def draw(self):
        c = self.canv
        c.saveState()
        y = 42
        xs = [10, 132, 254, 376]
        nodes = [
            ("main", "先拉最新", "#f8f1e4"),
            ("个人分支", "design/story/art", "#eef5ff"),
            ("本地验证", "Unity / 导表 / 打包", "#f1f8ef"),
            ("push", "交给负责人合并", "#fff0eb"),
        ]
        for i, node in enumerate(nodes):
            self.draw_node(c, xs[i], y, *node)
            if i < len(nodes) - 1:
                c.setStrokeColor(colors.HexColor("#9b7a48"))
                c.setLineWidth(1.2)
                c.line(xs[i] + 98, y + 21, xs[i + 1] - 6, y + 21)
                c.setFillColor(colors.HexColor("#9b7a48"))
                c.circle(xs[i + 1] - 6, y + 21, 2, stroke=0, fill=1)
        c.setFont(FONT_REGULAR, 8)
        c.setFillColor(colors.HexColor("#6b5a48"))
        c.drawString(10, 17, "规则：不要直接在 main 上乱改；每个岗位使用自己的工作分支，验证通过后再提交。")
        c.restoreState()


class MockUnityOpen(Flowable):
    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.height = 130
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.saveState()
        c.setFillColor(colors.HexColor("#f8f4ea"))
        c.setStrokeColor(colors.HexColor("#b88635"))
        c.roundRect(0, 0, self.width, self.height, 8, stroke=1, fill=1)
        c.setFillColor(colors.HexColor("#24160e"))
        c.roundRect(12, 82, self.width - 24, 32, 5, stroke=0, fill=1)
        c.setFillColor(colors.HexColor("#e5b85f"))
        c.setFont(FONT_BOLD, 12)
        c.drawString(24, 94, "Unity Hub")
        c.setFillColor(colors.HexColor("#fffaf0"))
        c.roundRect(20, 24, self.width - 40, 43, 5, stroke=0, fill=1)
        c.setStrokeColor(colors.HexColor("#d2a24b"))
        c.roundRect(20, 24, self.width - 40, 43, 5, stroke=1, fill=0)
        c.setFillColor(colors.HexColor("#4d321d"))
        c.setFont(FONT_BOLD, 10)
        c.drawString(33, 50, "Add / 添加项目")
        c.setFont(FONT_REGULAR, 8.5)
        c.drawString(33, 35, PROJECT_PATH)
        c.setFillColor(colors.HexColor("#6e3527"))
        c.roundRect(self.width - 132, 34, 92, 22, 4, stroke=0, fill=1)
        c.setFillColor(colors.white)
        c.setFont(FONT_BOLD, 8)
        c.drawCentredString(self.width - 86, 41, UNITY_VERSION)
        c.restoreState()


class CoverArt(Flowable):
    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.height = 188
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.saveState()
        c.setFillColor(colors.HexColor("#2a1810"))
        c.roundRect(0, 0, self.width, self.height, 12, stroke=0, fill=1)
        c.setFillColor(colors.HexColor("#5c2d22"))
        c.rect(0, 0, self.width, 52, stroke=0, fill=1)
        c.setFillColor(colors.HexColor("#d8ad5b"))
        c.setFont(FONT_BOLD, 18)
        c.drawString(24, 142, "明路 Unity 项目")
        c.setFont(FONT_BOLD, 30)
        c.drawString(24, 98, "新人安装手册")
        c.setFont(FONT_REGULAR, 10.5)
        c.drawString(26, 69, "GitHub 拉取 - Git LFS 资源 - Unity 打开 - 分支提交")
        c.setStrokeColor(colors.HexColor("#d8ad5b"))
        c.setLineWidth(2)
        c.line(24, 55, self.width - 24, 55)
        for i, label in enumerate(["注册账号", "克隆项目", "打开工程", "提交分支"]):
            x = self.width - 182 + i * 43
            c.setFillColor(colors.HexColor("#f9e2a2"))
            c.circle(x, 130 - i * 16, 14, stroke=0, fill=1)
            c.setFillColor(colors.HexColor("#2a1810"))
            c.setFont(FONT_BOLD, 8)
            c.drawCentredString(x, 127 - i * 16, str(i + 1))
            c.setFillColor(colors.HexColor("#f9e2a2"))
            c.setFont(FONT_REGULAR, 7)
            c.drawCentredString(x, 104 - i * 16, label)
        c.restoreState()


def page_canvas(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(colors.HexColor("#f7f0e3"))
    canvas.rect(0, 0, A4[0], A4[1], stroke=0, fill=1)
    canvas.setStrokeColor(colors.HexColor("#c8a25a"))
    canvas.setLineWidth(0.8)
    canvas.line(18 * mm, 17 * mm, A4[0] - 18 * mm, 17 * mm)
    canvas.setFont(FONT_REGULAR, 8)
    canvas.setFillColor(colors.HexColor("#6e5a42"))
    canvas.drawString(19 * mm, 11.5 * mm, "《明路》新人安装与拉取项目教程")
    canvas.drawRightString(A4[0] - 19 * mm, 11.5 * mm, f"第 {doc.page} 页")
    canvas.restoreState()


def section_title(text: str, styles):
    return Paragraph(text, styles["h1"])


def p(text: str, styles):
    return Paragraph(text, styles["body"])


def image_or_note(path: Path, width: float, caption: str, styles):
    if path.exists():
        img = Image(str(path))
        iw, ih = img.imageWidth, img.imageHeight
        scale = min(width / iw, 210 / ih)
        img.drawWidth = iw * scale
        img.drawHeight = ih * scale
        return [img, Paragraph(caption, styles["caption"]), Spacer(1, 6)]
    return [Callout("截图缺失", f"未找到截图：{path.name}", "red"), Spacer(1, 6)]


def table(data, widths, header=True):
    ts = TableStyle(
        [
            ("FONTNAME", (0, 0), (-1, -1), FONT_REGULAR),
            ("FONTSIZE", (0, 0), (-1, -1), 8),
            ("LEADING", (0, 0), (-1, -1), 11),
            ("GRID", (0, 0), (-1, -1), 0.45, colors.HexColor("#d4b979")),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#fffaf0")),
            ("TEXTCOLOR", (0, 0), (-1, -1), colors.HexColor("#2c2118")),
            ("LEFTPADDING", (0, 0), (-1, -1), 6),
            ("RIGHTPADDING", (0, 0), (-1, -1), 6),
            ("TOPPADDING", (0, 0), (-1, -1), 5),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ]
    )
    if header:
        ts.add("FONTNAME", (0, 0), (-1, 0), FONT_BOLD)
        ts.add("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#6e3527"))
        ts.add("TEXTCOLOR", (0, 0), (-1, 0), colors.white)
    return Table(data, colWidths=widths, style=ts, hAlign="LEFT")


def build_pdf() -> Path:
    register_fonts()
    styles = style_sheet()
    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=A4,
        leftMargin=19 * mm,
        rightMargin=19 * mm,
        topMargin=18 * mm,
        bottomMargin=22 * mm,
        title="《明路》新人安装与拉取项目教程 图文版",
        author="MingLu Project",
    )
    usable_width = A4[0] - doc.leftMargin - doc.rightMargin
    story = []

    story.append(CoverArt())
    story.append(Spacer(1, 12))
    story.append(Paragraph("给第一次加入项目的策划、美术、UI、关卡和程序同学使用", styles["subtitle"]))
    story.append(Spacer(1, 10))
    story.append(
        Callout(
            "先看这一条",
            "不要用 GitHub 的 Download ZIP。项目使用 Git LFS 管理图片、Excel 和美术资源，必须通过 Git 克隆并执行 git lfs pull。",
            "red",
        )
    )
    story.append(Spacer(1, 10))
    story.append(
        table(
            [
                ["项目", "要求"],
                ["本地路径", PROJECT_PATH],
                ["仓库地址", REPO_URL],
                ["Unity 版本", UNITY_VERSION],
                ["必装工具", "Git for Windows、Git LFS、Unity Hub、Unity Editor"],
                ["推荐空间", "D 盘预留 15 GB 以上"],
            ],
            [90, usable_width - 90],
        )
    )
    story.append(PageBreak())

    story.append(section_title("一、安装总流程", styles))
    story.append(
        StepMap(
            [
                ("准备账号", "注册 GitHub，并让负责人添加协作者权限"),
                ("装 Git", "安装 Git for Windows 和 Git LFS"),
                ("配置身份", "设置 user.name、user.email 和中文文件名显示"),
                ("克隆项目", "URL 和本地目录分开填，拉取 LFS 资源"),
                ("装 Unity", f"安装 Unity Hub 与 Editor {UNITY_VERSION}"),
                ("运行验证", "打开 Main.unity，进入标题界面和战棋工坊"),
                ("建立分支", "不同岗位从 main 建个人工作分支"),
                ("提交改动", "本地验证后 commit，再 push 到远程"),
            ]
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CardGrid(
            [
                ("最容易错的 1", "SourceTree 的 URL 只填仓库地址；本地目录填在目标路径。"),
                ("最容易错的 2", "首次 clone 后一定要执行 git lfs pull，否则图片和表格可能只是占位文本。"),
                ("最容易错的 3", "Unity 必须使用 2022.3.62f3，版本不一致会导致导入、UI 或资源表现异常。"),
                ("最容易错的 4", "Library、Temp、Logs、Builds 是本地生成物，正常不要提交。"),
            ]
        )
    )
    story.append(PageBreak())

    story.append(section_title("二、GitHub 权限与登录", styles))
    story.append(p("如果只是查看公共仓库，可以直接克隆；如果要提交改动，负责人需要先把你的 GitHub 账号加入 collaborator。", styles))
    story.append(
        CodeBlock(
            """
GitHub 仓库页面 -> Settings -> Collaborators -> Add people
推荐登录方式：Sign in with your browser
"""
        )
    )
    story.append(Spacer(1, 8))
    story.extend(
        image_or_note(
            IMAGES / "github_sign_in.png",
            180,
            "GitHub 登录窗口：新人优先点 Sign in with your browser",
            styles,
        )
    )
    story.append(
        Callout(
            "不要优先使用 Token",
            "除非已经知道如何创建 Personal Access Token，否则直接用浏览器登录最省事。登录成功后 Git Credential Manager 会记住授权。",
            "blue",
        )
    )
    story.append(PageBreak())

    story.append(section_title("三、安装 Git 和 Git LFS", styles))
    story.append(
        CardGrid(
            [
                ("Git for Windows", "从 git-scm.com 下载，安装时大部分选项保持默认。安装后重新打开 cmd。"),
                ("Git LFS", "从 git-lfs.com 安装。项目里的大图片、Excel、docx 等资源依赖它。"),
                ("VS Code / Visual Studio", "不是必须，但推荐安装，方便查看脚本、Markdown、CSV 和报错日志。"),
                ("检查命令", "cmd 中执行 git --version、git lfs version，能看到版本号才算成功。"),
            ],
            columns=2,
            tone="blue",
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        CodeBlock(
            """
git --version
git lfs version
git lfs install
"""
        )
    )
    story.append(Spacer(1, 7))
    story.append(
        CodeBlock(
            """
git config --global user.name "你的名字"
git config --global user.email "你的邮箱"
git config --global core.quotepath false
"""
        )
    )
    story.append(PageBreak())

    story.append(section_title("四、克隆项目：命令行方式", styles))
    story.append(p("推荐把项目放在 D 盘固定目录。这样团队教程、打包脚本和排查问题时的路径都一致。", styles))
    story.append(
        CodeBlock(
            rf"""
D:
mkdir D:\paotuan
cd /d D:\paotuan
git clone {REPO_URL} MingLuUnity
cd MingLuUnity
git lfs pull
"""
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CardGrid(
            [
                ("成功标志", "本地出现 D:\\paotuan\\MingLuUnity，并能看到 Assets、Packages、ProjectSettings、Docs 等目录。"),
                ("检查状态", "执行 git status，应显示 On branch main；执行 git lfs ls-files，应看到 png、xlsx、docx 等资源。"),
            ],
            columns=2,
            tone="green",
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CodeBlock(
            """
git status
git lfs ls-files
"""
        )
    )
    story.append(PageBreak())

    story.append(section_title("五、克隆项目：SourceTree 方式", styles))
    story.append(
        Callout(
            "关键点",
            "SourceTree 里的源路径 / URL 只填仓库地址，不要把 MingLuUnity 写进 URL。目标路径单独填本地目录。",
            "red",
        )
    )
    story.append(Spacer(1, 6))
    story.append(
        table(
            [
                ["位置", "应该填写"],
                ["源路径 / URL", REPO_URL],
                ["目标路径", PROJECT_PATH],
                ["名称", "MingLuUnity"],
            ],
            [95, usable_width - 95],
        )
    )
    story.append(Spacer(1, 6))
    story.extend(
        image_or_note(
            IMAGES / "github_repo_quick_setup.png",
            usable_width,
            "GitHub 仓库 Quick setup 页面：复制 HTTPS 地址时只复制 .git 结尾的 URL",
            styles,
        )
    )
    story.append(
        CodeBlock(
            rf"""
错误写法：
{REPO_URL} MingLuUnity

正确写法：
URL: {REPO_URL}
目标路径: {PROJECT_PATH}
"""
        )
    )
    story.append(PageBreak())

    story.append(section_title("六、安装并打开 Unity 工程", styles))
    story.append(p(f"Unity Editor 必须安装 {UNITY_VERSION}。第一次打开工程会导入资源，等 Unity 完成导入后再 Play。", styles))
    story.append(MockUnityOpen())
    story.append(Spacer(1, 8))
    story.append(
        CardGrid(
            [
                ("打开路径", PROJECT_PATH),
                ("打开场景", "Assets/Scenes/Main.unity"),
                ("点击运行", "Unity 顶部 Play，能进入《明路》标题界面即通过。"),
                ("不要提交", "Library、Temp、Logs、Builds、UserSettings 都是本地生成物。"),
            ],
            columns=2,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            [
                ["入口", "新人第一次需要确认"],
                ["新游戏", "创角界面能打开；不填名字时默认是“夏邑”。"],
                ["学院", "课程按钮、角色信息、事件入口能显示。"],
                ["剧情目录", "剧情能打开；条件不足会给明确提示。"],
                ["战略地图", "能看到省份和军团。"],
                ["战棋工坊", "能放单位、改地形、设置目标并开始测试。"],
            ],
            [85, usable_width - 85],
        )
    )
    story.append(PageBreak())

    story.append(section_title("七、日常更新和个人分支", styles))
    story.append(BranchDiagram())
    story.append(Spacer(1, 8))
    story.append(
        CodeBlock(
            rf"""
cd /d {PROJECT_PATH}
git switch main
git pull
git lfs pull
"""
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            [
                ["岗位", "分支示例"],
                ["策划表格", "design/你的名字-tables"],
                ["剧情文案", "story/你的名字-events"],
                ["美术资源", "assets/你的名字-art"],
                ["战棋关卡", "battle/你的名字-levels"],
                ["UI 调整", "ui/你的名字-layout"],
            ],
            [90, usable_width - 90],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CodeBlock(
            """
git switch main
git pull
git switch -c design/your-name-tables
git push -u origin design/your-name-tables
"""
        )
    )
    story.append(PageBreak())

    story.append(section_title("八、策划、美术和打包流程", styles))
    story.append(Paragraph("策划改表", styles["h2"]))
    story.append(
        CodeBlock(
            """
双击 导出配置表.bat
修改 DataTables/csv 里的 CSV
双击 检查配置表.bat
双击 回写配置表.bat
进 Unity Play 验证
git add .
git commit
git push
"""
        )
    )
    story.append(Spacer(1, 6))
    story.append(
        table(
            [
                ["目录/表", "用途"],
                ["DataTables/csv", "数值、剧情、UI 文案、任务、战棋配置。"],
                ["Assets/Resources/Art/Portraits", "角色立绘。"],
                ["Assets/Resources/Art/Scenes", "场景原画。"],
                ["Assets/Resources/Art/BattleUnits", "战棋单位序列帧。"],
                ["Assets/Resources/Art/UI", "UI 按钮和面板。"],
                ["一键打包安装包.bat", "生成本地测试安装包，输出到 Builds/Installers。"],
            ],
            [135, usable_width - 135],
        )
    )
    story.append(Spacer(1, 6))
    story.append(
        Callout(
            "美术提交规则",
            "Unity 资源必须连 .meta 一起提交。不要随便改资源文件名；改名后配置表和代码中的资源路径也要同步改。",
            "gold",
        )
    )
    story.append(PageBreak())

    story.append(section_title("九、提交自己的改动", styles))
    story.append(p("改完后先看状态，再提交。提交说明要写清楚这次改了什么，方便负责人 review 和以后回溯。", styles))
    story.append(
        CodeBlock(
            """
git status
git add .
git commit -m "补充第一章剧情触发条件"
git push
"""
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CardGrid(
            [
                ("推荐提交说明", "补充第一章剧情触发条件；调整步兵和骑兵基础数值；新增战棋工坊测试地图。"),
                ("不要这样写", "update、111、随便改改。这类说明无法判断内容，也不利于查问题。"),
                ("提交前检查", "Unity 能 Play；导表能通过；新增图片有 .meta；git status 没有误提交 Builds。"),
                ("遇到冲突", "不要乱点覆盖。先 git status，把冲突文件和你改了什么发给负责人。"),
            ],
            columns=2,
        )
    )
    story.append(PageBreak())

    story.append(section_title("十、常见错误速查", styles))
    story.append(
        CardGrid(
            [
                ("git 不是命令", "Git 没装好，或装完没重开 cmd。重装后重新打开命令行。"),
                ("git lfs 不是命令", "安装 Git LFS，执行 git lfs install，再执行 git lfs pull。"),
                ("图片变成小文本", "LFS 资源没拉下来。进项目目录执行 git lfs pull。"),
                ("URL rejected", "SourceTree 的 URL 写错。URL 后不要加本地文件夹名。"),
                ("Unity 资源丢失", "先 git lfs pull；仍不行就关 Unity、删 Library、重开工程。"),
                ("push 没权限", "让负责人把你的 GitHub 账号加入协作者。"),
            ],
            columns=2,
            tone="blue",
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        CodeBlock(
            """
新人完成安装后回报：
我已完成项目安装
本地路径：D:\\paotuan\\MingLuUnity
Unity 版本：2022.3.62f3
Git 分支：main
是否能进入标题界面：是/否
是否执行过 git lfs pull：是/否
遇到的问题：
"""
        )
    )

    doc.build(story, onFirstPage=page_canvas, onLaterPages=page_canvas)
    return OUTPUT


if __name__ == "__main__":
    out = build_pdf()
    print(out)
